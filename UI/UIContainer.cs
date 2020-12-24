using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.UI
{
  public abstract class UIContainer : IUIContainer
  {
    public List<IUIComponent> Children { get; } = new List<IUIComponent>();

    public abstract void Render();
    protected void RenderChildren()
    {
      foreach (var child in Children)
      {
        child.Render();
      }
    }
  }
}
