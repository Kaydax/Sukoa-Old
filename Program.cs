using System;
using System.Linq;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using ImGuiNET;
using Sukoa.UI;
using Sukoa.Renderer;
using System.Diagnostics;
using Sukoa.Components;
using Sukoa.MIDI;

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
      var gd = view.GraphicsDevice;

      var cl = gd.ResourceFactory.CreateCommandList();

      var imGui = new ImGuiView(gd, view.Window, gd.MainSwapchain.Framebuffer.OutputDescription, view.Width, view.Height);
      ImGui.GetIO().ConfigWindowsMoveFromTitleBarOnly = true;

      //Create ImGui Windows
      var menu = new UIMenu("Hello World!", new IUIComponent[] { new UIMenuItem("Test 1"), new UIMenuItem("Test 2", "CTRL+Z") });
      var mainmenu = new UIMainMenuBar(new IUIComponent[] { menu });

      var uiwindow = new UIWindow("Abstracted ImGui!", new IUIComponent[] { new UIText("Test Text"), new UICheckbox("Test Checkbox", false) });
      
      var pattern = new MIDIPattern();
      var pianoRollWindow = UIUtils.CreatePianoRollWindow(pattern, gd, imGui);

      // Initialize imgui UI
      var uihost = dispose.Add(new UIHost(new IUIComponent[] { mainmenu, uiwindow, pianoRollWindow }));
      
      Stopwatch frameTimer = new Stopwatch();
      frameTimer.Start();

      // Main application loop
      while (view.Exists)
      {
        if (!view.Exists) { break; }
        imGui.Update((float)frameTimer.Elapsed.TotalSeconds, view.Width, view.Height);
        frameTimer.Reset();
        frameTimer.Start();

        cl.Begin();

        // Compute UI elements, render canvases
        uihost.Render(cl);
        ImGui.ShowDemoWindow();

        ImGui.Text(ImGui.GetIO().Framerate.ToString());

        imGui.UpdateViewIO(view);

        cl.SetFramebuffer(gd.MainSwapchain.Framebuffer);
        cl.ClearColorTarget(0, new RgbaFloat(clearColor.X, clearColor.Y, clearColor.Z, 1f));
        imGui.Render(gd, cl);
        cl.End();
        gd.SubmitCommands(cl);
        gd.SwapBuffers(gd.MainSwapchain);
        imGui.SwapExtraWindows(gd);
      }

      dispose.Dispose();
    }
  }
}