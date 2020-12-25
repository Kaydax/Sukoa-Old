using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sukoa.Renderer;
using Veldrid;
using ImGuiNET;
using System.Numerics;

namespace Sukoa.UI
{
  public abstract class UICanvas : IUIComponent, IDisposable
  {
    public RenderCanvas Canvas { get; private set; }
    ImGuiView ImGuiView { get; }
    Func<Vector2> ComputeSize { get; }

    protected ResourceFactory Factory => GraphicsDevice.ResourceFactory;
    protected GraphicsDevice GraphicsDevice { get; }

    DisposeGroup dispose = new DisposeGroup();
    IntPtr imageBind;

    protected UICanvas(GraphicsDevice gd, ImGuiView imGuiView, Func<Vector2> computeSize)
    {
      ComputeSize = computeSize;
      ImGuiView = imGuiView;
      GraphicsDevice = gd;

      Canvas = MakeCanvasFrom(8, 8);
      imageBind = ImGuiView.GetOrCreateImGuiBinding(Factory, Canvas.TextureView);
    }

    public virtual void Render(CommandList cl)
    {
      var imgSize = ComputeSize();

      int IntImageSizeX = (int)Math.Floor(imgSize.X);
      int IntImageSizeY = (int)Math.Floor(imgSize.Y);

      if(Canvas.Width != IntImageSizeX || Canvas.Height != IntImageSizeY)
      {
        Canvas = MakeCanvasFrom(IntImageSizeX, IntImageSizeY);
        imageBind = ImGuiView.GetOrCreateImGuiBinding(Factory, Canvas.TextureView);
      }

      ImGui.Image(imageBind, new Vector2(IntImageSizeX, IntImageSizeY));

      ProcessInputs();

      RenderToCanvas(cl);
    }

    protected virtual void ProcessInputs()
    {

    }

    protected abstract void RenderToCanvas(CommandList cl);

    public void Dispose()
    {
      dispose.Dispose();
    }

    // Helper functions
    RenderCanvas MakeCanvasFrom(int width, int height)
    {
      return dispose.Replace(Canvas, new RenderCanvas(Factory, width, height));
    }
  }
}
