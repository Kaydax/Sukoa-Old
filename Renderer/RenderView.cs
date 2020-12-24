using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Sukoa.Renderer
{
  public class RenderView : IDisposable
  {
    public GraphicsDevice GraphicsDevice { get; }
    public Sdl2Window Window { get; }

    DisposeGroup dispose = new DisposeGroup();

    public RenderView()
    {
      WindowCreateInfo windowCI = new WindowCreateInfo()
      {
        X = 100,
        Y = 100,
        WindowWidth = 960,
        WindowHeight = 540,
        WindowTitle = "Veldrid Tutorial"
      };
       
      Window = VeldridStartup.CreateWindow(ref windowCI);

      GraphicsDeviceOptions options = new GraphicsDeviceOptions
      {
        PreferStandardClipSpaceYDirection = true,
        PreferDepthRangeZeroToOne = true
      };

      GraphicsDevice = dispose.Add(VeldridStartup.CreateGraphicsDevice(Window, options, GraphicsBackend.Vulkan));

      Window.Resized += () =>
      {
        GraphicsDevice.MainSwapchain.Resize((uint)Window.Width, (uint)Window.Height);
      };
    }

    public void Dispose()
    {
      dispose.Dispose();
    }
  }
}
