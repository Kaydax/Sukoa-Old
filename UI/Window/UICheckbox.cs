using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using Veldrid;

namespace Sukoa.UI
{
  public class UICheckbox : UIComponent
  {
    public UICheckbox(string label, UIProperty<bool> @checked)
    {
      Label = label;
      Checked = @checked;
    }

    public string Label { get; set; }
    public UIProperty<bool> Checked { get; set; } = new UIValueProperty<bool>(false);

    public override void Render(CommandList cl)
    {
      var check = Checked.Value;

      ImGui.Checkbox(Label, ref check);
      if(check != Checked.Value) Checked.Set(check);
    }
  }
}