using MIDIModificationFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.MIDI
{
  public static class Extensions
  {
    public static SNote ToSukoaNote(this Note n) => new SNote(n.Start, n.End, n.Velocity);
    //public static Note ToMMFNoteList(this MIDIPattern pattern) => new SNote(n.Start, n.End, n.Velocity);
  }
}
