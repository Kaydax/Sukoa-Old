using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using Veldrid;

namespace Sukoa.UI
{
  public class UIMenu : UIContainer
  {
    public UIMenu(string label, IEnumerable<IUIComponent> children) : base(children)
    {
      Label = label;
    }

    public UIMenu(string label) : this(label, Enumerable.Empty<IUIComponent>()) { }

    public String Label { get; set; }

    public override void Render(CommandList cl)
    {
      if (ImGui.BeginMenu(Label))
      {
        RenderChildren(cl);
        ImGui.EndMenu();
      }
    }
  }
}
