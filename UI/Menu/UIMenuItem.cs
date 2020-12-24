using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using Veldrid;

namespace Sukoa.UI
{
  public class UIMenuItem : UIComponent
  {
    public UIMenuItem(string label, string shortcut = null)
    {
      Label = label;
      Shortcut = shortcut;
    }

    public String Label { get; set; }
    public String Shortcut { get; set; }

    public override void Render(CommandList cl)
    {
      if (ImGui.MenuItem(Label, Shortcut)) { }
    }
  }
}
