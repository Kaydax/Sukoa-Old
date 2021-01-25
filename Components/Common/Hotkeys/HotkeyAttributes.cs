using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace Sukoa.Components.Common.Hotkeys
{
  [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
  public class HotkeyName : Attribute
  {
    public HotkeyName(string name)
    {
      Name = name;
    }

    public string Name { get; }
  }

  [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
  public class HotkeyTrigger : Attribute
  {
    public HotkeyTrigger(Key key)
    {
      Key = key;
    }

    public Key Key { get; }
    public bool Shift { get; set; }
    public bool Ctrl { get; set; }
    public bool Alt { get; set; }
  }
}
