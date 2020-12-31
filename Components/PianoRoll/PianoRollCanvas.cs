using Sukoa.Renderer;
using Sukoa.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.SPIRV;
using System.Numerics;
using ImGuiNET;
using Sukoa.MIDI;
using Sukoa.Util;
using Rectangle = Sukoa.Util.Rectangle;
using Sukoa.Components.PianoRoll.Interactions;
using System.Threading;

namespace Sukoa.Components
{
  public class PianoRollCanvas : UICanvas
  {
    struct VertexPositionColor
    {
      public Vector2 Position; // This is the position, in normalized device coordinates.
      public RgbaFloat Color; // This is the color of the vertex.
      public VertexPositionColor(Vector2 position, RgbaFloat color)
      {
        Position = position;
        Color = color;
      }
      public const uint SizeInBytes = 24;
    }

    BufferList<VertexPositionColor>[] Buffers { get; }
    DeviceBuffer ProjMatrix { get; set; }
    ResourceLayout Layout { get; set; }
    ResourceSet MainResourceSet { get; set; }
    Shader[] Shaders { get; set; }
    Pipeline Pipeline { get; set; }
    PianoRollPattern PatternHandler { get; }

    IPianoRollInteraction CurrentInteraction { get; set; }
    public int[] FirstRenderNote { get; } = new int[256];
    public double LastRenderLeft { get; set; } = 0;

    const string VertexCode = @"
      #version 450

      layout(location = 0) in vec2 Position;
      layout(location = 1) in vec4 Color;

      layout (binding = 0) uniform ProjectionMatrixBuffer
      {
          mat4 projection_matrix;
      };

      layout(location = 0) out vec4 fsin_Color;

      void main()
      {
          gl_Position = projection_matrix * vec4(Position, 0, 1);
          fsin_Color = Color;
      }";

    const string FragmentCode = @"
      #version 450

      layout(location = 0) in vec4 fsin_Color;
      layout(location = 0) out vec4 fsout_Color;

      void main()
      {
          fsout_Color = fsin_Color;
      }";

    DisposeGroup dispose = new DisposeGroup();

    public PianoRollCanvas(GraphicsDevice gd, ImGuiView view, Func<Vector2> computeSize, PianoRollPattern pattern) : base(gd, view, computeSize)
    {
      Buffers = new BufferList<VertexPositionColor>[257]; // first buffer for general purpose, others for keys
      for(int i = 0; i < 257; i++)
        Buffers[i] = dispose.Add(new BufferList<VertexPositionColor>(gd, 6 * 2048 * 16, new[] { 0, 3, 2, 0, 2, 1 }));
      PatternHandler = pattern;
      CurrentInteraction = new PianoRollInteractionIdle(PatternHandler);

      ProjMatrix = Factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
      dispose.Add(ProjMatrix);

      VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
        new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
        new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));

      Layout = Factory.CreateResourceLayout(new ResourceLayoutDescription(
          new ResourceLayoutElementDescription("ProjectionMatrixBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)));
      dispose.Add(Layout);

      ShaderDescription vertexShaderDesc = new ShaderDescription(
        ShaderStages.Vertex,
        Encoding.UTF8.GetBytes(VertexCode),
        "main");
      ShaderDescription fragmentShaderDesc = new ShaderDescription(
        ShaderStages.Fragment,
        Encoding.UTF8.GetBytes(FragmentCode),
        "main");

      Shaders = dispose.AddArray(Factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc));

      var pipelineDescription = new GraphicsPipelineDescription();
      pipelineDescription.BlendState = BlendStateDescription.SingleAlphaBlend;
      pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
        depthTestEnabled: true,
        depthWriteEnabled: true,
        comparisonKind: ComparisonKind.LessEqual);
      pipelineDescription.RasterizerState = new RasterizerStateDescription(
        cullMode: FaceCullMode.Front,
        fillMode: PolygonFillMode.Solid,
        frontFace: FrontFace.Clockwise,
        depthClipEnabled: true,
        scissorTestEnabled: false);
      pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleList;
      pipelineDescription.ResourceLayouts = new ResourceLayout[] { Layout };
      pipelineDescription.ShaderSet = new ShaderSetDescription(
        vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
        shaders: Shaders);
      pipelineDescription.Outputs = Canvas.FrameBuffer.OutputDescription;

      Pipeline = Factory.CreateGraphicsPipeline(pipelineDescription);

      MainResourceSet = Factory.CreateResourceSet(new ResourceSetDescription(Layout, ProjMatrix));
    }

    protected override void ProcessInputs()
    {
      base.ProcessInputs();

      CurrentInteraction.Act();
      while(CurrentInteraction.NextInteraction != null)
      {
        CurrentInteraction = CurrentInteraction.NextInteraction;
      }
    }

    protected override void RenderToCanvas(CommandList cl)
    {
      cl.SetFramebuffer(Canvas.FrameBuffer);

      cl.ClearColorTarget(0, RgbaFloat.Clear);

      Matrix4x4 mvp = Matrix4x4.Identity * Matrix4x4.CreateScale(2, 2, 1) * Matrix4x4.CreateScale(1.0f / this.Canvas.Width, 1.0f / this.Canvas.Height, 1) * Matrix4x4.CreateTranslation(-1, -1, 0) * Matrix4x4.CreateScale(1, -1, 1);

      GraphicsDevice.UpdateBuffer(ProjMatrix, 0, ref mvp);

      cl.SetPipeline(Pipeline);
      cl.SetGraphicsResourceSet(0, MainResourceSet);

      foreach (var x in Buffers)
        x.Reset();

      PatternHandler.Update();

      var viewFrame = PatternHandler.ViewFrame;

      var canvasSize = ImGui.GetItemRectSize();

      Rectangle ScaleRectSides(Rectangle rect)
      {
        return new Rectangle(
          (float)(viewFrame.TransformYToOutside(rect.Top) * canvasSize.Y),
          (float)(viewFrame.TransformXToOutside(rect.Right) * canvasSize.X),
          (float)(viewFrame.TransformYToOutside(rect.Bottom) * canvasSize.Y),
          (float)(viewFrame.TransformXToOutside(rect.Left) * canvasSize.X)
        );
      }

      bool QuadVisible(Rectangle rect)
      {
        return rect.Top < canvasSize.Y && rect.Bottom > 0 && rect.Left < canvasSize.X && rect.Right > 0;
      }

      void PushQuad(int bufferIdx, Rectangle rect, RgbaFloat col)
      {
        if(!QuadVisible(rect))
          return;
        float top = (float)rect.Top, bottom = (float)rect.Bottom, left = (float)rect.Left, right = (float)rect.Right;
        var buffer = Buffers[bufferIdx];
        buffer.Push(cl, new VertexPositionColor(new Vector2(left, top), col));
        buffer.Push(cl, new VertexPositionColor(new Vector2(right, top), col));
        buffer.Push(cl, new VertexPositionColor(new Vector2(right, bottom), col));
        buffer.Push(cl, new VertexPositionColor(new Vector2(left, bottom), col));
      }

      void PushSelectionRectangle(Rectangle rect)
      {
        var scaled = ScaleRectSides(rect);
        PushQuad(0, scaled, new RgbaFloat(0.8f, 0.2f, 0.2f, 0.7f));
      }

      void PushNote(int note, Rectangle rect, RgbaFloat col)
      {
        var borderCol = new RgbaFloat(col.ToVector4() * new Vector4(0.4f, 0.4f, 0.4f, 1));
        rect = ScaleRectSides(rect);
        PushQuad(note + 1, rect, borderCol);
        rect.Top += 1;
        rect.Bottom -= 1;
        rect.Right -= 1;
        rect.Left += 1;
        if(rect.IsValid)
        {
          PushQuad(note + 1, rect, col);
        }
      }

      var pattern = PatternHandler.Pattern;

      var nc = 0;

      var renderLeft = viewFrame.EaseLeft;
      Parallel.For(0, pattern.Notes.Length, key =>
      {
        var noteKey = pattern.Notes[key];
        int noteOffset = FirstRenderNote[key];
        if(LastRenderLeft > renderLeft)
        {
          for(noteOffset = 0; noteOffset < noteKey.Count; noteOffset++)
          {
            if(noteKey[noteOffset].End > renderLeft)
              break;
          }
          FirstRenderNote[key] = noteOffset;
        }
        else if(LastRenderLeft < renderLeft)
        {
          for(; noteOffset < noteKey.Count; noteOffset++)
          {
            if(noteKey[noteOffset].End > renderLeft)
              break;
          }
          FirstRenderNote[key] = noteOffset;
        }
        //foreach(var note in noteKey)
        if(viewFrame.TransformYToOutside(key + 1) < 0 || viewFrame.TransformYToOutside(key) > 1)
          return;
        for(int i = noteOffset; i < noteKey.Count; i++)
        {
          var note = noteKey[i];
          var rect = new Rectangle(key, (float)note.End, key + 1, (float)note.Start);

          if(viewFrame.TransformXToOutside(rect.Left) > 1)
            break;
          if(viewFrame.TransformXToOutside(rect.Right) < 0)
            continue;

          // Don't render selected notes yet, render them in the next pass
          if(PatternHandler.IsNoteSelected(note))
          {
            continue;
          }

          var isSelected = false;
          if(PatternHandler.IsNoteInSelectionRectangle(note, key))
          {
            isSelected = true;
          }

          var noteCol = new RgbaFloat(0.2f, 0.2f, 0.9f, 1);
          if(isSelected)
          {
            noteCol = new RgbaFloat(0.9f, 0.2f, 0.2f, 1);
          }

          Interlocked.Increment(ref nc);
          PushNote(key, rect, noteCol);
        }
      });
      LastRenderLeft = renderLeft;

      var selectionPosOffset = PatternHandler.SelectionPosOffset;
      foreach(var selected in PatternHandler.SelectedNotes)
      {
        var note = selected.Note;
        var key = selected.Key;
        var noteCol = new RgbaFloat(0.9f, 0.2f, 0.2f, 1);
        PushNote(key, new Rectangle(key, (float)note.End, key + 1, (float)note.Start).OffsetBy(selectionPosOffset), noteCol);
      }

      if(PatternHandler.SelectionRectangle != null)
      {
        nc++;
        PushSelectionRectangle(PatternHandler.SelectionRectangle ?? new Rectangle());
      }

      foreach(var x in Buffers)
        x.Flush(cl);

      ImGui.SetCursorPos(ImGui.GetCursorStartPos());
      ImGui.Text($"Notes rendering: {nc}");
      ImGui.Text($"Selected notes: {PatternHandler.SelectedNotes.Count()}");
    }
  }
}
