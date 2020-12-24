using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace Sukoa.UI
{
  public class UIMainMenuBar : UIContainer
  {
    public override void Render()
    {
      if (ImGui.BeginMainMenuBar())
      {
        RenderChildren();
        ImGui.EndMainMenuBar();
      }
    }
  }
}
