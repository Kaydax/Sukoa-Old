using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.SPIRV;

namespace Sukoa.Renderer
{
  class CanvasView
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

    GraphicsDevice GraphicsDevice { get; }
    OutputDescription OutputDescription { get; }

    CommandList CommandList { get; set; }
    DeviceBuffer VertexBuffer { get; set; }
    DeviceBuffer IndexBuffer { get; set; }
    Shader[] Shaders { get; set; }
    Pipeline Pipeline { get; set; }

    const string VertexCode = @"
      #version 450

      layout(location = 0) in vec2 Position;
      layout(location = 1) in vec4 Color;

      layout(location = 0) out vec4 fsin_Color;

      void main()
      {
          gl_Position = vec4(Position, 0, 1);
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

    public CanvasView(GraphicsDevice gd, OutputDescription od, int width, int height)
    {
      GraphicsDevice = gd;
      OutputDescription = od;

      CreateDeviceResources();
    }

    public void CreateDeviceResources()
    {
      ResourceFactory factory = GraphicsDevice.ResourceFactory;

      VertexPositionColor[] quadVertices =
      {
        new VertexPositionColor(new Vector2(-0.75f, 0.75f), RgbaFloat.Red),
        new VertexPositionColor(new Vector2(0.75f, 0.75f), RgbaFloat.Green),
        new VertexPositionColor(new Vector2(-0.75f, -0.75f), RgbaFloat.Blue),
        new VertexPositionColor(new Vector2(0.75f, -0.75f), RgbaFloat.Yellow)
      };

      ushort[] quadIndices = { 0, 1, 2, 3 };

      VertexBuffer = factory.CreateBuffer(new BufferDescription(4 * VertexPositionColor.SizeInBytes, BufferUsage.VertexBuffer));
      IndexBuffer = factory.CreateBuffer(new BufferDescription(4 * sizeof(ushort), BufferUsage.IndexBuffer));

      GraphicsDevice.UpdateBuffer(VertexBuffer, 0, quadVertices);
      GraphicsDevice.UpdateBuffer(IndexBuffer, 0, quadIndices);

      VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
        new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
        new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));

      ShaderDescription vertexShaderDesc = new ShaderDescription(
        ShaderStages.Vertex,
        Encoding.UTF8.GetBytes(VertexCode),
        "main");
      ShaderDescription fragmentShaderDesc = new ShaderDescription(
        ShaderStages.Fragment,
        Encoding.UTF8.GetBytes(FragmentCode),
        "main");

      Shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);

      GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
      pipelineDescription.BlendState = BlendStateDescription.SingleOverrideBlend;
      pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
        depthTestEnabled: true,
        depthWriteEnabled: true,
        comparisonKind: ComparisonKind.LessEqual);
      pipelineDescription.RasterizerState = new RasterizerStateDescription(
        cullMode: FaceCullMode.Back,
        fillMode: PolygonFillMode.Solid,
        frontFace: FrontFace.Clockwise,
        depthClipEnabled: true,
        scissorTestEnabled: false);
      pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
      pipelineDescription.ResourceLayouts = System.Array.Empty<ResourceLayout>();
      pipelineDescription.ShaderSet = new ShaderSetDescription(
        vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
        shaders: Shaders);
      pipelineDescription.Outputs = OutputDescription;

      Pipeline = factory.CreateGraphicsPipeline(pipelineDescription);
    }

    public void Render(CommandList cl)
    {
      cl.ClearColorTarget(0, RgbaFloat.Black);
      cl.SetVertexBuffer(0, VertexBuffer);
      cl.SetIndexBuffer(IndexBuffer, IndexFormat.UInt16);
      cl.SetPipeline(Pipeline);
      cl.DrawIndexed(
          indexCount: 4,
          instanceCount: 1,
          indexStart: 0,
          vertexOffset: 0,
          instanceStart: 0);
    }
  }
}
