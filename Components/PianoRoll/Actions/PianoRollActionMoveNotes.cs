using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.Components.PianoRoll.Actions
{
  public class PianoRollActionMoveNotes : PianoRollAction
  {
    public List<SelectedSNote> NotesToMove { get; }

    public double Ticks { get; }
    public int Keys { get; }

    public PianoRollActionMoveNotes(PianoRollPattern pattern, IEnumerable<SelectedSNote> notesToMove, double ticks, int keys) : base(pattern)
    {
      NotesToMove = new List<SelectedSNote>(notesToMove);
      Ticks = ticks;
      Keys = keys;
    }

    protected override void ApplyInternal()
    {

    }

    protected override void UndoInternal()
    {
      throw new NotImplementedException();
    }
  }
}
