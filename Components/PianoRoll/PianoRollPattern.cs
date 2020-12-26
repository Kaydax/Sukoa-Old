using Sukoa.MIDI;
using Sukoa.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.Components
{
  public class PianoRollPattern
  {
    public MIDIPattern Pattern { get; }

    public HashSet<SNote> SelectedNotes { get; } = new HashSet<SNote>();
    public bool HasSelectedNotes => SelectedNotes.Count != 0;

    public SmoothZoomView ViewFrame { get; } = new SmoothZoomView(0, 128, 0, 100)
    {
      MaxBottom = 128,
      MaxTop = 0,
      MaxLeft = 0,
    };

    public PianoRollPattern(MIDIPattern pattern)
    {
      Pattern = pattern;
    }

    public SNote GetNoteAtLocation(Vector2 location)
    {
      var row = (int)Math.Floor(location.Y);
      if(row < 0 || row > Pattern.Notes.Length) return null;
      var notes = Pattern.Notes[row];
      
      // TODO: Start with binary search for first closest note start, instead of the end of the array
      for(int i = notes.Count - 1; i >= 0; i--)
      {
        var note = notes[i];
        if(note.Start < location.X && note.End > location.X)
        {
          return note;
        }
      }
      return null;
    }

    public Vector2 GetPositionInside(Vector2 outside)
    {
      return new Vector2((float)ViewFrame.TransformXToInside(outside.X), (float)ViewFrame.TransformYToInside(outside.Y));
    }

    public void SelectNote(SNote note)
    {
      SelectedNotes.Add(note);
    }

    public void DeselectNote(SNote note)
    {
      if(SelectedNotes.Contains(note)) SelectedNotes.Remove(note);
    }

    public void DeselectAllNotes()
    {
      SelectedNotes.Clear();
    }
  }
}
