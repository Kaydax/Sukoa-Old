using Sukoa.MIDI;
using Sukoa.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.Components.PianoRoll
{
  public partial class MIDIPatternConnect
  {
    public Rectangle? SelectionRectangle { get; private set; } = null;

    public bool IsNoteInSelectionRectangle(SNote note, float key)
    {
      if(SelectionRectangle == null) return false;
      return IsNoteInRectangle(note, key, SelectionRectangle ?? new Rectangle());
    }

    public void SetSelectionRectangle(Rectangle rect)
    {
      SelectionRectangle = rect;
    }

    public void ClearSelectionRectangle()
    {
      SelectionRectangle = null;
    }
  }
}
