using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using Veldrid;

namespace Sukoa.UI
{
  public class UIText : UIComponent
  {
    public UIText(string text)
    {
      Text = text;
    }

    public string Text { get; set; }

    public override void Render(CommandList cl)
    {
      ImGui.Text(Text);
    }
  }
}
