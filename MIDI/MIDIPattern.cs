using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MIDIModificationFramework;

namespace Sukoa.MIDI
{
  public class MIDIPattern
  {
    public List<Note> Notes { get; } = new List<Note>();

    public void GenNotes()
    {
      var file = new MidiFile("D:/Midis/scale-flip.mid");

      Notes.AddRange(file.IterateTracks().Select(t => t.ChangePPQ(file.PPQ, 1).ExtractNotes()).MergeAll());
    }
  }
}
