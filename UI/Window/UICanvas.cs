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

    ResourceFactory Factory { get; }

    DisposeGroup dispose = new DisposeGroup();
    IntPtr imageBind;


    protected UICanvas(ResourceFactory factory, ImGuiView imGuiView, Func<Vector2> computeSize)
    {
      ComputeSize = computeSize;
      ImGuiView = imGuiView;
      Factory = factory;

      Canvas = MakeCanvasFrom(8, 8);
      imageBind = ImGuiView.GetOrCreateImGuiBinding(Factory, Canvas.TextureView);
    }

    public virtual void Render(CommandList cl)
    {
      var imgSize = ComputeSize();

      int IntImageSizeX = (int)Math.Floor(imgSize.X);
      int IntImageSizeY = (int)Math.Floor(imgSize.Y);

      if (Canvas.Width != IntImageSizeX || Canvas.Height != IntImageSizeY)
      {
        Canvas = MakeCanvasFrom(IntImageSizeX, IntImageSizeY);
        imageBind = ImGuiView.GetOrCreateImGuiBinding(Factory, Canvas.TextureView);
      }

      ImGui.Image(imageBind, imgSize);

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
