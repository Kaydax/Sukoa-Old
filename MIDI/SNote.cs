using Sukoa.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.MIDI
{
  public class SNote : IComparable
  {
    double length;

    public double Start { get; set; }
    public byte Velocity { get; set; }

    public double End
    {
      get => Start + length;
      set
      {
        Length = value - Start;
      }
    }

    public double Length
    {
      get => length;
      set
      {
        if(value < -0.00000001)
          throw new ArgumentException("Note can not have a negative length");
        length = value;
      }
    }

    public void SetStartOnly(double newStart)
    {
      double newLength = End - newStart;
      Start = newStart;
      Length = newLength;
    }

    public SNote(double start, double end, byte vel)
    {
      this.Start = start;
      this.Length = end - start;
      this.Velocity = vel;
    }

    public virtual SNote Clone()
    {
      return new SNote(Start, End, Velocity);
    }

    public int CompareTo(object? obj)
    {
      return obj is SNote n ? Start.CompareTo(n.Start) : 0;
    }
  }

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

  public static class SelectedSNoteExtension
  {
    public static IEnumerable<SelectedSNote> ToSelectedSNotes(this PerKeyArray<IEnumerable<SNote>> notes)
    {
      for(int i = 0; i < notes.Length; i++)
      {
        var key = notes[i];
        foreach(var n in key)
        {
          yield return new SelectedSNote(n, i);
        }
      }
    }
  }
}
