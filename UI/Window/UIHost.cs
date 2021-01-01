using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace Sukoa.UI
{
  public class UIHost : UIContainer
  {
    public UIHost(IEnumerable<IUIComponent> children) : base(children) { }
    public UIHost() : this(Enumerable.Empty<IUIComponent>()) { }

    public override void Render(CommandList cl)
    {
      RenderChildren(cl);
    }
  }
}
