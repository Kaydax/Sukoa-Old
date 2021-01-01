using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MIDIModificationFramework;
using Sukoa.Util;

namespace Sukoa.MIDI
{
  public class MIDIPattern
  {
    public List<SNote>[] Notes { get; } = SUtil.CreatePerKeyItemArray(() => new List<SNote>());

    public void GenNotes()
    {
      var file = new MidiFile("D:/Midis/Clubstep.mid");

      var allNotes = file.IterateTracks().Select(t => t.ChangePPQ(file.PPQ, 1).ExtractNotes()).MergeAll().ToArray();

      foreach(var n in allNotes)
      {
        Notes[n.Key].Add(n.ToSukoaNote());
      }
    }
  }
}
