using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace Sukoa.Components.Common.Hotkeys
{
  public enum GlobalHotkey
  {
    [HotkeyName("Undo")]
    [HotkeyTrigger(Key.Z, Ctrl = true)]
    Undo,

    [HotkeyName("Redo")]
    [HotkeyTrigger(Key.Y, Ctrl = true)]
    [HotkeyTrigger(Key.Z, Ctrl = true, Shift = true)]
    Redo,
  }
}
