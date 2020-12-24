using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace Sukoa.UI
{
  public class UIMenu : UIContainer
  {
    public UIMenu(string label)
    {
      Label = label;
    }

    public String Label { get; set; }

    public override void Render()
    {
      if(ImGui.BeginMenu(Label))
      {
        RenderChildren();
        ImGui.EndMenu();
      }
    }
  }
}
