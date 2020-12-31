using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.UI
{
  public class UIProperty<T>
  {
    private Action<T> set;

    public UIProperty(Func<T> get, Action<T> set)
    {
      Get = get;
      this.set = set;
      Set = set;
    }

    public event EventHandler<T>? ValueChanged;

    public Func<T> Get { get; protected set; }
    public Action<T> Set
    {
      get => set;
      protected set
      {
        set = val =>
        {
          value(val);
          ValueChanged?.Invoke(this, val);
        };
      }
    }
    public T Value => Get();
  }
}
