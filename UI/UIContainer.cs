using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace Sukoa.UI
{
  public abstract class UIContainer : IUIContainer
  {
    public List<IUIComponent> Children { get; } = new List<IUIComponent>();

    public abstract void Render(CommandList cl);

    protected void RenderChildren(CommandList cl)
    {
      foreach (var child in Children)
      {
        child.Render(cl);
      }
    }

    public virtual void Dispose()
    {
      foreach (var child in Children)
      {
        child.Dispose();
      }
    }
  }
}
