using Sukoa.MIDI;
using Sukoa.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.Components.PianoRoll
{
  public partial class PianoRollPattern
  {
    HashSet<SelectedSNote> SelectedNotesHashset { get; } = new HashSet<SelectedSNote>();
    public IEnumerable<SelectedSNote> SelectedNotes => SelectedNotesHashset;
    public bool HasSelectedNotes => SelectedNotesHashset.Count != 0;
    public Rectangle? SelectedNotesBounds { get; private set; } = null;

    public void SelectNote(SNote note, int key)
    {
      SelectNote(new SelectedSNote(note, key));
    }

    public void SelectNoteRange(IEnumerable<SelectedSNote> notes)
    {
      foreach(var n in notes) SelectNote(n);
    }

    public void SelectNote(SelectedSNote note)
    {
      if(SelectedNotesBounds.HasValue)
      {
        SelectedNotesBounds = SelectedNotesBounds.Value.CombineBoundsWith(note.GetBoundsRectangle());
      }
      else
      {
        SelectedNotesBounds = note.GetBoundsRectangle();
      }
      SelectedNotesHashset.Add(note);
    }

    public bool IsNoteSelected(SNote note)
    {
      return IsNoteSelected(new SelectedSNote(note, -1));
    }

    public bool IsNoteSelected(SelectedSNote note)
    {
      return SelectedNotesHashset.Contains(note);
    }

    public void DeselectNote(SNote note)
    {
      var selected = new SelectedSNote(note, -1);
      DeselectNote(selected);
    }

    public void DeselectNote(SelectedSNote note)
    {
      if(SelectedNotesHashset.Contains(note)) SelectedNotesHashset.Remove(note);
      UpdateSelectedNoteBoundsRectangle();
    }

    public void DeselectAllNotes()
    {
      SelectedNotesHashset.Clear();
      UpdateSelectedNoteBoundsRectangle();
    }

    void UpdateSelectedNoteBoundsRectangle()
    {
      if(SelectedNotesHashset.Count == 0)
      {
        SelectedNotesBounds = null;
        return;
      }

      var rect = SelectedNotesHashset.First().GetBoundsRectangle();
      foreach(var n in SelectedNotesHashset)
      {
        rect = rect.CombineBoundsWith(n.GetBoundsRectangle());
      }

      SelectedNotesBounds = rect;
    }
  }
}
