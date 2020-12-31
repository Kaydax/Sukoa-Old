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
  public struct SelectedSNote
  {
    public SelectedSNote(SNote note, int key)
    {
      Note = note;
      Key = key;
    }

    public SNote Note { get; }
    public int Key { get; }

    public static bool operator ==(SelectedSNote a, SelectedSNote b) => a.Equals(b);
    public static bool operator !=(SelectedSNote a, SelectedSNote b) => !a.Equals(b);

    public override bool Equals(object? obj)
    {
      return obj is SelectedSNote note && EqualityComparer<SNote>.Default.Equals(Note, note.Note);
    }

    public override int GetHashCode()
    {
      return Note.GetHashCode();
    }

    public Rectangle GetBoundsRectangle()
    {
      return new Rectangle(Key, Note.End, Key + 1, Note.Start);
    }
  }

  class SelectionDragOffset
  {
    VelocityEase YOffsetEase = new VelocityEase(0) { Duration = 0.05, Slope = 1, VelocityPower = 1 };
    VelocityEase XOffsetEase = new VelocityEase(0) { Duration = 0.0, Slope = 2, VelocityPower = 2 };

    public Vector2d EaseOffset { get; private set; } = new Vector2d();
    public Vector2d TrueOffset => new Vector2d(XOffsetEase.End, YOffsetEase.End);

    public void Update()
    {
      EaseOffset = new Vector2d(XOffsetEase.GetValue(), YOffsetEase.GetValue());
    }

    public void SetEnd(Vector2d end)
    {
      XOffsetEase.SetEnd(end.X);
      YOffsetEase.SetEnd(end.Y);
    }

    public void ForceReset()
    {
      XOffsetEase.ForceValue(0);
      YOffsetEase.ForceValue(0);
    }
  }

  public class PianoRollPattern
  {
    public MIDIPattern Pattern { get; }

    HashSet<SelectedSNote> SelectedNotesHashset { get; } = new HashSet<SelectedSNote>();
    public IEnumerable<SelectedSNote> SelectedNotes => SelectedNotesHashset;
    public bool HasSelectedNotes => SelectedNotesHashset.Count != 0;
    public Rectangle? SelectedNotesBounds { get; private set; } = null;

    public Rectangle? SelectionRectangle { get; private set; } = null;

    public Vector2d SelectionPosOffset => SelectionOffsetPosEase.EaseOffset;
    SelectionDragOffset SelectionOffsetPosEase { get; } = new SelectionDragOffset();

    public double NoteSnapInterval { get; set; } = 1;

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

    public static bool IsNoteInRectangle(SNote note, float key, Rectangle rect)
    {
      if(rect.Left > note.End || rect.Right < note.Start)
        return false;


      if(rect.Top > key + 1 || rect.Bottom < key)
        return false;

      return true;
    }

    public void Update()
    {
      ViewFrame.Update();
      SelectionOffsetPosEase.Update();
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

    public void SelectNote(SNote note, int key)
    {
      SelectNote(new SelectedSNote(note, key));
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

    public bool IsNoteInSelectionRectangle(SNote note, float key)
    {
      if(SelectionRectangle == null) return false;
      return IsNoteInRectangle(note, key, SelectionRectangle ?? new Rectangle());
    }

    public bool IsNoteSelected(SNote note)
    {
      return IsNoteSelected(new SelectedSNote(note, -1));
    }

    public bool IsNoteSelected(SelectedSNote note)
    {
      return SelectedNotesHashset.Contains(note);
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

    public void SetSelectionRectangle(Rectangle rect)
    {
      SelectionRectangle = rect;
    }

    public void ClearSelectionRectangle()
    {
      SelectionRectangle = null;
    }

    public void SetSelectionPosOffset(Vector2d start, Vector2d offset)
    {
      var yOffset = Math.Round(Math.Floor(offset.Y + start.Y) - Math.Floor(start.Y));
      var xOffset = offset.X;

      if(SelectedNotesBounds.HasValue)
      {
        var dist = (SelectedNotesBounds.Value.Left + xOffset) % NoteSnapInterval;
        xOffset -= dist; 
      }


      if(SelectedNotesBounds.HasValue)
      {
        var bounds = SelectedNotesBounds.Value;
        if(bounds.Left + xOffset < 0)
        {
          xOffset = -bounds.Left;
        }
        if(bounds.Top + yOffset < 0)
        {
          yOffset = -bounds.Top;
        }
        if(bounds.Bottom + yOffset > 128)
        {
          yOffset = 128 - bounds.Bottom;
        }
      }


      SelectionOffsetPosEase.SetEnd(new Vector2d(xOffset, yOffset));
    }

    public void ClearSelectionPosOffset()
    {
      SelectionOffsetPosEase.ForceReset();
    }

    public void ApplySelectionPosOffset()
    {
      double xOffset = SelectionOffsetPosEase.TrueOffset.X;
      int keyOffset = (int)Math.Round(SelectionOffsetPosEase.TrueOffset.Y);

      var separatedSelection = new HashSet<SNote>[256];
      for(int i = 0; i < separatedSelection.Length; i++) separatedSelection[i] = new HashSet<SNote>();

      foreach(var n in SelectedNotesHashset)
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

        Pattern.Notes[i] = Pattern.Notes[i].Where(n => !prevSelection.Contains(n)).Concat(newSelection).ToList();

        if(newSelectionCount != 0)
        {
          Pattern.Notes[i].Sort();
        }
      });

      var editedSelection = SelectedNotesHashset.Select(n => new SelectedSNote(n.Note, n.Key + keyOffset)).ToList();
      DeselectAllNotes();
      foreach(var n in editedSelection)
      {
        SelectNote(n);
      }

      ClearSelectionPosOffset();
    }
  }
}
