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
  public interface IPianoRollAction
  {
    bool Applied { get; }

    // public long EstimatedRamUsage { get; }

    void Apply();
    void Undo();
  }

  public abstract class MIDIPatternAction : IPianoRollAction
  {
    public bool Applied { get; private set; } = false;
    public MIDIPatternConnect PianoRollPattern { get; }

    public MIDIPatternAction(MIDIPatternConnect pattern)
    {
      PianoRollPattern = pattern;
    }

    public void Apply()
    {
      if(Applied) throw new Exception("Can't apply an applied action again");
      ApplyInternal();
      Applied = true;
    }

    public void Undo()
    {
      if(!Applied) throw new Exception("Can't undo an unapplied action");
      UndoInternal();
      Applied = false;
    }

    protected abstract void ApplyInternal();
    protected abstract void UndoInternal();
  }
}
