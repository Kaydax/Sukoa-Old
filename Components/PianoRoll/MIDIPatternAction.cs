using Sukoa.Components.Common;
using Sukoa.MIDI;
using Sukoa.Util;
using Sukoa.Util.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.Components.PianoRoll
{
  public abstract class MIDIPatternAction : UserAction
  {
    public MIDIPatternConnect PianoRollPattern { get; }

    public MIDIPatternAction(MIDIPatternConnect pattern)
    {
      PianoRollPattern = pattern;
    }
  }
}
