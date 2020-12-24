using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace Sukoa.UI
{
  public class UICheckbox : IUIComponent
  {
    public UICheckbox(string label, UIProperty<bool> @checked)
    {
      Label = label;
      Checked = @checked;
    }

    public UICheckbox(string label, bool @checked) : this(label, new UIValueProperty<bool>(@checked))
    {
      
    }

    public String Label { get; set; }
    public UIProperty<bool> Checked { get; set; } = new UIValueProperty<bool>(false);

    public void Render()
    {
      var check = Checked.Value;

      ImGui.Checkbox(Label, ref check);
      if(check != Checked.Value) Checked.Set(check);
    }
  }
}