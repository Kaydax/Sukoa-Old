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
    public string Name { get; set; }
    public UIProperty<bool> Open { get; set; }
    public ImGuiWindowFlags Flags { get; set; }

    public UIWindow(string name, UIProperty<bool> open, ImGuiWindowFlags flags, IEnumerable<IUIComponent> children) : base(children)
    {
      Name = name;
      Open = open;
      Flags = flags;
    }

    public UIWindow(string name, IEnumerable<IUIComponent> children) : this(name, new UIValueProperty<bool>(true), ImGuiWindowFlags.None, children) { }
    public UIWindow(string name) : this(name, Enumerable.Empty<IUIComponent>()) { }

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
