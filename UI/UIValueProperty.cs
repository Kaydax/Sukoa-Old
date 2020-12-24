using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.UI
{
  public class UIValueProperty<T> : UIProperty<T>
  {
    T value;

    public UIValueProperty(T value) : base(() => default(T), v => { })
    {
      this.value = value;
      this.Get = () => this.value;
      this.Set = v => this.value = v;
    }
  }
}
