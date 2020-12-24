using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using Veldrid;

namespace Sukoa.UI
{
  public class UIWindow : UIContainer
  {
    public UIProperty<bool> Open { get; set; } = new UIValueProperty<bool>(true);
    public ImGuiWindowFlags Flags { get; set; } = ImGuiWindowFlags.None;
    public String Name { get; set; } = "Unset";

    public override void Render(CommandList cl)
    {
      var open = Open.Value;

      if (!open) return;

      ImGui.Begin(Name, ref open, Flags);
      if (open != Open.Value) Open.Set(open);
      RenderChildren(cl);
      ImGui.End();
    }
  }
}
