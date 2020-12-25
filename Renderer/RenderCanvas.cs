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
      Width = width;
      Height = height;

      Texture = factory.CreateTexture(TextureDescription.Texture2D((uint)width, (uint)height, 1, 1, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.RenderTarget | TextureUsage.Sampled)); ;
      TextureView = factory.CreateTextureView(Texture);
      FrameBuffer = factory.CreateFramebuffer(new FramebufferDescription(null, Texture));

      dispose.Add(Texture);
      dispose.Add(TextureView);
      dispose.Add(FrameBuffer);
    }

    public Texture Texture { get; }
    public Framebuffer FrameBuffer { get; }
    public TextureView TextureView { get; }
    public int Width { get; }
    public int Height { get; }

    DisposeGroup dispose = new DisposeGroup();

    public void Dispose()
    {
      dispose.Dispose();
    }
  }
}
