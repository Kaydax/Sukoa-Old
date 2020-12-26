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

    BufferList<VertexPositionColor> Buffer { get; }
    DeviceBuffer ProjMatrix { get; set; }
    ResourceLayout Layout { get; set; }
    ResourceSet MainResourceSet { get; set; }
    Shader[] Shaders { get; set; }
    Pipeline Pipeline { get; set; }
    PianoRollPattern PatternHandler { get; }

    IPianoRollAction CurrentAction { get; set; } = null;

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
      Buffer = dispose.Add(new BufferList<VertexPositionColor>(gd, 6 * 2048 / 16, new[] { 0, 3, 2, 0, 2, 1 }));
      PatternHandler = pattern;

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
      pipelineDescription.BlendState = BlendStateDescription.SingleOverrideBlend;
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

      var rect = ImGui.GetItemRectSize();

      var io = ImGui.GetIO();

      var mousePos = ImGui.GetMousePos() - ImGui.GetWindowPos() - ImGui.GetCursorStartPos();
      var mouseRelativePos = mousePos / ImGui.GetItemRectSize();

      if(CurrentAction != null)
      {
        CurrentAction.Act();
        while (CurrentAction != null && CurrentAction.NextAction != CurrentAction)
        {
          CurrentAction = CurrentAction.NextAction;
        }
      }
      else
      {
        if(ImGui.IsItemHovered())
        {
          if(ImGui.IsMouseDown(ImGuiMouseButton.Left))
          {
            CurrentAction = new PianoRollActionMouseDown(PatternHandler);
            while(CurrentAction != null && CurrentAction.NextAction != CurrentAction)
            {
              CurrentAction = CurrentAction.NextAction;
            }
          }
        }
      }

      if(ImGui.IsItemHovered())
      {
        if(io.MouseWheel != 0)
        {
          var zoom = Math.Pow(1.2, -io.MouseWheel);

          var viewFrame = PatternHandler.ViewFrame;
          if(io.KeyCtrl)
          {
            viewFrame.ZoomHorizontalAt(mouseRelativePos.X, zoom);
          }
          else if(io.KeyAlt)
          {
            viewFrame.ZoomVerticalAt(mouseRelativePos.Y, zoom);
          }
          else if(io.KeyShift)
          {
            viewFrame.ScrollHorizontalBy(viewFrame.TrueWidth / 8 * -io.MouseWheel);
          }
          else
          {
            viewFrame.ScrollVerticalBy(viewFrame.TrueHeight / 8 * -io.MouseWheel);
          }
        }
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

      Buffer.Reset();

      var viewFrame = PatternHandler.ViewFrame;

      viewFrame.Update();

      var canvasSize = ImGui.GetItemRectSize();

      void ScaleRectSides(ref double top, ref double right, ref double bottom, ref double left)
      {
        top = viewFrame.TransformYToOutside(top) * canvasSize.Y;
        bottom = viewFrame.TransformYToOutside(bottom) * canvasSize.Y;
        left = viewFrame.TransformXToOutside(left) * canvasSize.X;
        right = viewFrame.TransformXToOutside(right) * canvasSize.X;
      }

      bool IsRectIllegal(double top, double right, double bottom, double left)
      {
        return top > bottom || left > right;
      }

      void PushQuad(float top, float right, float bottom, float left, RgbaFloat col)
      {
        if(top > canvasSize.Y || bottom < 0 || left > canvasSize.X || right < 0) return;
        Buffer.Push(cl, new VertexPositionColor(new Vector2(left, top), col));
        Buffer.Push(cl, new VertexPositionColor(new Vector2(right, top), col));
        Buffer.Push(cl, new VertexPositionColor(new Vector2(right, bottom), col));
        Buffer.Push(cl, new VertexPositionColor(new Vector2(left, bottom), col));
      }

      void PushNote(double top, double right, double bottom, double left, RgbaFloat col)
      {
        var borderCol = new RgbaFloat(col.ToVector4() * new Vector4(0.4f, 0.4f, 0.4f, 1));
        ScaleRectSides(ref top, ref right, ref bottom, ref left);
        PushQuad((float)top, (float)right, (float)bottom, (float)left, borderCol);
        top += 1;
        bottom -= 1;
        right -= 1;
        left += 1;
        if(!IsRectIllegal(top, right, bottom, left))
        {
          PushQuad((float)top, (float)right, (float)bottom, (float)left, col);
        }
      }

      var pattern = PatternHandler.Pattern;

      for(int key = 0; key < pattern.Notes.Length; key++)
      {
        var noteKey = pattern.Notes[key];
        foreach(var note in noteKey)
        {
          var noteCol = new RgbaFloat(0.2f, 0.2f, 0.9f, 1);
          if(PatternHandler.SelectedNotes.Contains(note))
          {
            noteCol = new RgbaFloat(0.9f, 0.2f, 0.2f, 1);
          }
          PushNote(key, note.End, key + 1, note.Start, noteCol);
        }
      }

      Buffer.Flush(cl);
    }
  }
}
