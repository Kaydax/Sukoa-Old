using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using Sukoa.Components;
using Sukoa.Components.PianoRoll;
using Sukoa.Components.Project;
using Sukoa.MIDI;
using Sukoa.Renderer;
using Veldrid;

namespace Sukoa.UI
{
  public static class UIUtils
  {
    public static UIWindow CreatePianoRollWindow(ProjectConnect projectConnect,  MIDIPattern pattern, GraphicsDevice gd, ImGuiView imGui)
    {
      var menu = new UIMenu("Retard Menu", new IUIComponent[] { new UIMenuItem("Snap Size") });
      var menuBar = new UIMenuBar(new IUIComponent[] { menu });
      pattern.GenNotes();
      var pianoPattern = new MIDIPatternConnect(projectConnect, pattern);
      var canvas = new MIDIPatternIO(gd, imGui, ImGui.GetContentRegionAvail, pianoPattern);
      var window = new UIWindow("PianoRoll", new UIValueProperty<bool>(true), ImGuiWindowFlags.MenuBar, new IUIComponent[] { menuBar, canvas });

      return window;
    }
  }
}
