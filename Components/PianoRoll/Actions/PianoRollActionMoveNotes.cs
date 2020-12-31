using Sukoa.MIDI;
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
      double xOffset = Ticks;
      int keyOffset = Keys;

      var pattern = PianoRollPattern.Pattern;

      var separatedSelection = new HashSet<SNote>[256];
      for(int i = 0; i < separatedSelection.Length; i++) separatedSelection[i] = new HashSet<SNote>();

      foreach(var n in NotesToMove)
      {
        separatedSelection[n.Key].Add(n.Note);
      }

      Parallel.For(0, 256, i =>
      {
        var newSelection = Enumerable.Empty<SNote>();
        var newSelectionCount = 0;
        var offsettedKey = i - keyOffset;
        if(offsettedKey >= 0 && offsettedKey < 256)
        {
          newSelectionCount = separatedSelection[offsettedKey].Count;
          newSelection = separatedSelection[offsettedKey].Select(n =>
          {
            n.Start += xOffset;
            return n;
          });
        }
        var prevSelection = separatedSelection[i];

        if(prevSelection.Count == 0 && newSelectionCount == 0) return;

        pattern.Notes[i] = pattern.Notes[i].Where(n => !prevSelection.Contains(n)).Concat(newSelection).ToList();

        if(newSelectionCount != 0)
        {
          pattern.Notes[i].Sort();
        }
      });

      var editedSelection = NotesToMove.Select(n => new SelectedSNote(n.Note, n.Key + keyOffset)).ToList();
      PianoRollPattern.DeselectAllNotes();
      PianoRollPattern.SelectNote(editedSelection);
    }

    protected override void UndoInternal()
    {

    }
  }
}
