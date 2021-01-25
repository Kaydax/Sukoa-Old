using ImGuiNET;
using Sukoa.MIDI;
using Sukoa.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.Components.PianoRoll.Interactions
{
  class MIDIPatternInteractionMouseDown : MIDIPatternInteraction
  {
    Vector2d ClickLocation { get; }
    SelectedSNote? ClickedNote { get; }

    public MIDIPatternInteractionMouseDown(MIDIPatternConnect pianoRollPattern) : base(pianoRollPattern)
    {
      if(ImGui.GetIO().KeyShift)
      {
        ContinueWith(new MIDIPatternInteractionMouseShiftDown(PianoRollPattern));
        return;
      }
      ClickLocation = GetMousePos();

      ClickedNote = PianoRollPattern.GetNoteAtLocation(ClickLocation);
      if(ClickedNote == null || !PianoRollPattern.IsNoteSelected(ClickedNote.Value))
      {
        PianoRollPattern.DeselectAllNotes();
      }

      if(ClickedNote != null)
      {
        PianoRollPattern.SelectNote(ClickedNote.Value);
      }
    }

    public override IPianoRollInteraction? DoInteraction()
    {
      base.DoInteraction();
      if(!ImGui.IsMouseDown(ImGuiMouseButton.Left))
      {
        return new MIDIPatternInteractionIdle(PianoRollPattern);
      }
      if(ClickLocation != GetMousePos())
      {
        if(ClickedNote != null)
        {
          return new MIDIPatternInteractionMoveSelectedNotes(PianoRollPattern, ClickLocation);
        }
        else
        {
          return new MIDIPatternInteractionSelectionRectangle(PianoRollPattern, ClickLocation);
        }
      }
      return null;
    }
  }
}
