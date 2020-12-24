using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace Sukoa.Renderer
{
  public class RenderCanvas : IRenderTexture, IRenderTarget, IRenderSize, IDisposable
  {
    public RenderCanvas(ResourceFactory factory, int width, int height)
    {
      Texture = factory.CreateTexture(TextureDescription.Texture2D(1024, 1024, 1, 1, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.RenderTarget | TextureUsage.Sampled)); ;
      FrameBuffer = factory.CreateFramebuffer(new FramebufferDescription(null, Texture));
      Width = width;
      Height = height;

      dispose.Add(Texture);
      dispose.Add(FrameBuffer);
    }

    public Texture Texture { get; }
    public Framebuffer FrameBuffer { get; }
    public int Width { get; }
    public int Height { get; }

    DisposeGroup dispose = new DisposeGroup();

    public void Dispose()
    {
      dispose.Dispose();
    }
  }
}
