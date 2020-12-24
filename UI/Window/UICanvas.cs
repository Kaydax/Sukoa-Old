using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sukoa.Renderer;
using Veldrid;
using ImGuiNET;

namespace Sukoa.UI
{
  public abstract class UICanvas : IUIComponent, IDisposable
  {
    RenderCanvas Canvas { get; set; }
    ImGuiView ImGuiView { get; }

    DisposeGroup dispose = new DisposeGroup();

    protected UICanvas(ImGuiView imGuiView)
    {
      ImGuiView = imGuiView;
    }

    public void Render()
    {
      float imageSizeX = ImGui.GetWindowSize().X;
      float imageSizeY = ImGui.GetWindowSize().Y;

      int IntImageSizeX = (int)Math.Floor(imageSizeX);
      int IntImageSizeY = (int)Math.Floor(imageSizeY);

      if(Canvas.Width != IntImageSizeX || Canvas.Height != IntImageSizeY)
      {
        Canvas = dispose.Replace(Canvas, new RenderCanvas(gd.ResourceFactory, IntImageSizeX, IntImageSizeY));
        imageBind = imGui.GetOrCreateImGuiBinding(gd.ResourceFactory, Canvas.Texture);
      }
    }

    public void Dispose()
    {
      throw new NotImplementedException();
    }
  }
}
