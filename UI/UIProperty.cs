using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.UI
{
  public class UIProperty<T>
  {
    public static implicit operator UIProperty<T>(T val) => new UIValueProperty<T>(val);
    public static implicit operator T(UIProperty<T> property) => property.Value;

    private Action<T> set;

    public UIProperty(Func<T> get, Action<T> set)
    {
      Get = get;
      this.set = set;
      Set = set;
    }

    public UIProperty(Func<T> get) : this(get, v => throw new NotSupportedException())
    { }

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
