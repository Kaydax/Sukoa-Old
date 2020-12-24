using System;
using System.Linq;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using ImGuiNET;
using Sukoa.UI;
using Sukoa.Renderer;

namespace Sukoa
{
  class Program
  {
    // private static Sdl2Window window;
    // private static GraphicsDevice gd;
    private static CommandList cl;
    private static ImGuiView imGui;
    private static CanvasView canvas;
    private static UIWindow uiwindow = new UIWindow();
    private static UIMainMenuBar mainmenu = new UIMainMenuBar();

    private static Vector3 clearColor = new Vector3(0.45f, 0.55f, 0.6f);

    private static DisposeGroup dispose = new DisposeGroup();

    static void Main(string[] args)
    {
      var menu = new UIMenu("Hello World!");
      uiwindow.Name = "Abstracted ImGui!";
      uiwindow.Children.Add(new UIText("Test Text"));
      uiwindow.Children.Add(new UICheckbox("Test Checkbox", false));
      mainmenu.Children.Add(menu);
      menu.Children.Add(new UIMenuItem("Test 1"));
      menu.Children.Add(new UIMenuItem("Test 2", "CTRL+Z"));

      // Create window, GraphicsDevice, and all resources necessary for the demo.
      // VeldridStartup.CreateWindowAndGraphicsDevice(
      //     new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "ImGui.NET Sample Program"),
      //     new GraphicsDeviceOptions(true, null, true),
      //     out window,
      //     out gd);

      var view = new RenderView();

      var window = view.Window;
      var gd = view.GraphicsDevice;

      window.Resized += () =>
      {
        gd.MainSwapchain.Resize((uint)window.Width, (uint)window.Height);
        imGui.WindowResized(window.Width, window.Height);
      };
      cl = gd.ResourceFactory.CreateCommandList();
      imGui = new ImGuiView(gd, gd.MainSwapchain.Framebuffer.OutputDescription, window.Width, window.Height);

      //Texture colorTarget = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(1024, 1024, 1, 1, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.RenderTarget | TextureUsage.Sampled));
      //Framebuffer fb = gd.ResourceFactory.CreateFramebuffer(new FramebufferDescription(null, colorTarget));

      RenderCanvas buffer = new RenderCanvas(gd.ResourceFactory, 256, 256);
      dispose.Add(buffer);

      IntPtr imageBind = imGui.GetOrCreateImGuiBinding(gd.ResourceFactory, buffer.Texture);

      canvas = new CanvasView(gd, buffer.FrameBuffer.OutputDescription, 0, 0);

      // Main application loop
      while (window.Exists)
      {
        InputSnapshot snapshot = window.PumpEvents();
        if (!window.Exists) { break; }
        imGui.Update(1f / 60f, snapshot); // Feed the input events to our ImGui view, which passes them through to ImGui.

        uiwindow.Render();
        mainmenu.Render();

        ImGui.Begin("Canvas Test");
        

        ImGui.Image(imageBind, new Vector2(buffer.Width, buffer.Height));
        ImGui.End();

        cl.Begin();
        cl.SetFramebuffer(buffer.FrameBuffer);
        canvas.Render(cl);
        cl.SetFramebuffer(gd.MainSwapchain.Framebuffer);
        cl.ClearColorTarget(0, new RgbaFloat(clearColor.X, clearColor.Y, clearColor.Z, 1f));
        imGui.Render(gd, cl);
        cl.End();
        gd.SubmitCommands(cl);
        gd.SwapBuffers(gd.MainSwapchain);
      }

      // Clean up Veldrid resources
      gd.WaitForIdle();
      imGui.Dispose();
      cl.Dispose();
      dispose.Dispose();

      view.Dispose();
    }
  }
}