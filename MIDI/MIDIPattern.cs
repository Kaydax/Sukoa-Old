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
    public List<SNote>[] Notes { get; } = new List<SNote>[256];

    public void GenNotes()
    {
      var file = new MidiFile("D:/Midis/Clubstep.mid");

      for(int i = 0; i < Notes.Length; i++) Notes[i] = new List<SNote>();

      var allNotes = file.IterateTracks().Select(t => t.ChangePPQ(file.PPQ, 1).ExtractNotes()).MergeAll().ToArray();

      foreach(var n in allNotes)
      {
        Notes[n.Key].Add(n.ToSukoaNote());
      }
    }
  }
}
