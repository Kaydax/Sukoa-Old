using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MIDIModificationFramework;
using Sukoa.Util;
using Sukoa.Util.Exceptions;

namespace Sukoa.MIDI
{
  public class MIDIPattern
  {
    public PerKeyArray<List<SNote>> Notes { get; } = PerKeyArray.Create(() => new List<SNote>());

    public PerKeyArray<HashSet<SNote>> FetchSelectedNotes(PerKeyArray<HashSet<int>> selector)
    {
      var rets = PerKeyArray.Create(() => new HashSet<SNote>());
      Parallel.For(0, Constants.KeyCount, i =>
      {
        var select = selector[i];
        var ret = rets[i];
        var key = Notes[i];

        foreach(var idx in select)
        {
          try
          {
            ret.Add(key[idx]);
          }
          catch
          {
            throw new DataIntegrityException("Invalid indexes were passed into the note selection fetcher");
          }
        }
      });

      return rets;
    }

    public void RemoveSelectedNotes(PerKeyArray<HashSet<int>> selector)
    {
      Parallel.For(0, Constants.KeyCount, i =>
      {
        var select = selector[i];
        Notes[i] = Notes[i].Where((n, i) => !select.Contains(i)).ToList();
      });
    }

    public void RemoveSelectedNotes(PerKeyArray<HashSet<SNote>> selector)
    {
      Parallel.For(0, Constants.KeyCount, i =>
      {
        var select = selector[i];
        Notes[i] = Notes[i].Where((n) => !select.Contains(n)).ToList();
      });
    }

    public void InjectNotes(PerKeyArray<IEnumerable<SNote>> notes)
    {
      Parallel.For(0, Constants.KeyCount, i =>
      {
        var inject = notes[i];
        Notes[i] = Notes[i].Concat(inject).ToList();
        Notes[i].Sort();
      });
    }

    public PerKeyArray<HashSet<int>> GetNoteLocations(IEnumerable<SelectedSNote> selection)
    {
      var separatedSelection = PerKeyArray.Create(() => new List<SNote>());
      foreach(var n in selection)
      {
        separatedSelection[n.Key].Add(n.Note);
      }

      return GetNoteLocations(separatedSelection.Map(k => k.AsEnumerable()));
    }

    public PerKeyArray<HashSet<int>> GetNoteLocations(PerKeyArray<IEnumerable<SNote>> separatedSelection)
    {
      var locations = PerKeyArray.Create(() => new HashSet<int>());
      Parallel.For(0, Constants.KeyCount, i =>
      {
        var selection = separatedSelection[i];
        var notes = Notes[i];
        var locationsKey = locations[i];

        foreach(var s in selection)
        {
          var idx = notes.IndexOf(s);
          if(idx == -1) throw new DataIntegrityException("Invalid notes were passed into the note location finder");
          locationsKey.Add(idx);
        }
      });
      return locations;
    }


    ////////////////////////
    // TEMPORARY
    ////////////////////////
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
