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
    public override void Render(CommandList cl)
    {
      RenderChildren(cl);
    }
  }
}
