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
    public static bool IsNoteInRectangle(SNote note, float key, Rectangle rect)
    {
      if(rect.Left > note.End || rect.Right < note.Start)
        return false;


      if(rect.Top > key + 1 || rect.Bottom < key)
        return false;

      return true;
    }

    public IEnumerable<SelectedSNote> GetNotesInRectangle(Rectangle rect)
    {
      for(int key = 0; key < Pattern.Notes.Length; key++)
      {
        var keyNotes = Pattern.Notes[key];
        foreach(var note in keyNotes)
        {
          if(note.Start > rect.Right) break;
          if(IsNoteInRectangle(note, key, rect))
          {
            yield return new SelectedSNote(note, key);
          }
        }
      }
    }

    public SelectedSNote? GetNoteAtLocation(Vector2d location)
    {
      var row = (int)Math.Floor(location.Y);
      if(row < 0 || row > Pattern.Notes.Length) return null;
      var notes = Pattern.Notes[row];

      bool IsNoteIntersectingPointX(SNote note, double x)
      {
        return note.Start < x && note.End > x;
      }

      foreach(var n in SelectedNotesHashset)
      {
        if(n.Key == row && IsNoteIntersectingPointX(n.Note, location.X))
        {
          return n;
        }
      }

      // TODO: Start with binary search for first closest note start, instead of the end of the array
      for(int i = notes.Count - 1; i >= 0; i--)
      {
        var note = notes[i];
        if(IsNoteIntersectingPointX(note, location.X))
        {
          return new SelectedSNote(note, row);
        }
      }
      return null;
    }

    public Vector2d GetPositionInside(Vector2d outside)
    {
      return new Vector2d(ViewFrame.TransformXToInside(outside.X), ViewFrame.TransformYToInside(outside.Y));
    }
  }
}
