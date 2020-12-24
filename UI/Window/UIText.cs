using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace Sukoa.UI
{
  public class UIText : IUIComponent
  {
    public UIText(string text)
    {
      Text = text;
    }

    public String Text { get; set; }

    public void Render()
    {
      ImGui.Text(Text);
    }
  }
}
