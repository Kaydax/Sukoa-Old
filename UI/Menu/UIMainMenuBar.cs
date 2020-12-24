using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using Veldrid;

namespace Sukoa.UI
{
  public class UIMainMenuBar : UIContainer
  {
    public override void Render(CommandList cl)
    {
      if (ImGui.BeginMainMenuBar())
      {
        RenderChildren(cl);
        ImGui.EndMainMenuBar();
      }
    }
  }
}
