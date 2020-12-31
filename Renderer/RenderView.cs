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
  public class RenderView : IDisposable, IRenderSize, IRenderTarget
  {
    public GraphicsDevice GraphicsDevice { get; }
    public Sdl2Window Window { get; }

    public int Width => Window.Width;
    public int Height => Window.Height;

    public bool Exists => Window.Exists;

    public Framebuffer FrameBuffer => GraphicsDevice.SwapchainFramebuffer;

    Dictionary<SDL_SystemCursor, SDL_Cursor> cursorMap = new Dictionary<SDL_SystemCursor, SDL_Cursor>();

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

      void CreateAndAddCursor(SDL_SystemCursor type)
      {
        var cursor = Sdl2Native.SDL_CreateSystemCursor(type);
        cursorMap.Add(type, cursor);
      }
      CreateAndAddCursor(SDL_SystemCursor.Arrow);
      CreateAndAddCursor(SDL_SystemCursor.Crosshair);
      CreateAndAddCursor(SDL_SystemCursor.Hand);
      CreateAndAddCursor(SDL_SystemCursor.IBeam);
      CreateAndAddCursor(SDL_SystemCursor.No);
      CreateAndAddCursor(SDL_SystemCursor.SizeAll);
      CreateAndAddCursor(SDL_SystemCursor.SizeNESW);
      CreateAndAddCursor(SDL_SystemCursor.SizeNS);
      CreateAndAddCursor(SDL_SystemCursor.SizeNWSE);
      CreateAndAddCursor(SDL_SystemCursor.SizeWE);
      CreateAndAddCursor(SDL_SystemCursor.Wait);
      CreateAndAddCursor(SDL_SystemCursor.WaitArrow);

      GraphicsDevice = dispose.Add(VeldridStartup.CreateGraphicsDevice(Window, options, GraphicsBackend.Direct3D11));

      Window.Resized += () =>
      {
        GraphicsDevice.MainSwapchain.Resize((uint)Window.Width, (uint)Window.Height);
      };
    }

    public void SetCursor(SDL_SystemCursor cursor)
    {
      Sdl2Native.SDL_SetCursor(cursorMap[cursor]);
    }

    public void Dispose()
    {
      dispose.Dispose();
    }
  }
}
