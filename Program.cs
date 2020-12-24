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
    private static Vector3 clearColor = new Vector3(0.45f, 0.55f, 0.6f);

    static void Main(string[] args)
    {
      var dispose = new DisposeGroup();

      // Initialize window and imgui
      var view = dispose.Add(new RenderView());
      var window = view.Window;
      var gd = view.GraphicsDevice;

      var cl = gd.ResourceFactory.CreateCommandList();
      var imGui = new ImGuiView(gd, gd.MainSwapchain.Framebuffer.OutputDescription, window.Width, window.Height);
      window.Resized += () =>
      {
        gd.MainSwapchain.Resize((uint)window.Width, (uint)window.Height);
        imGui.WindowResized(window.Width, window.Height);
      };

      // Initialize imgui UI
      var uihost = new UIHost();

      var uiwindow = dispose.Add(new UIWindow());
      uiwindow.Name = "Abstracted ImGui!";
      uiwindow.Children.Add(new UIText("Test Text"));
      uiwindow.Children.Add(new UICheckbox("Test Checkbox", false));
      uihost.Children.Add(uiwindow);

      var mainmenu = dispose.Add(new UIMainMenuBar());
      var menu = new UIMenu("Hello World!");
      menu.Children.Add(new UIMenuItem("Test 1"));
      menu.Children.Add(new UIMenuItem("Test 2", "CTRL+Z"));
      mainmenu.Children.Add(menu);
      uihost.Children.Add(mainmenu);

      var canvasWindow = dispose.Add(new UIWindow());
      var testCanvas = new TestCanvas(gd, imGui, ImGui.GetContentRegionAvail);
      canvasWindow.Name = "Test canvas renderer";
      canvasWindow.Children.Add(testCanvas);
      uihost.Children.Add(canvasWindow);

      // Main application loop
      while (window.Exists)
      {
        InputSnapshot snapshot = window.PumpEvents();
        if (!window.Exists) { break; }
        imGui.Update(1f / 60f, snapshot);

        cl.Begin();

        // Compute UI elements, render canvases
        uihost.Render(cl);

        cl.SetFramebuffer(gd.MainSwapchain.Framebuffer);
        cl.ClearColorTarget(0, new RgbaFloat(clearColor.X, clearColor.Y, clearColor.Z, 1f));
        imGui.Render(gd, cl);
        cl.End();
        gd.SubmitCommands(cl);
        gd.SwapBuffers(gd.MainSwapchain);
      }

      dispose.Dispose();
    }
  }
}