using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace Sukoa.UI
{
  public class UIMenuItem : IUIComponent
  {
    public UIMenuItem(string label, string shortcut = null)
    {
      Label = label;
      Shortcut = shortcut;
    }

    public String Label { get; set; }
    public String Shortcut { get; set; }

    public void Render()
    {
      if(ImGui.MenuItem(Label, Shortcut)) {}
    }
  }
}
