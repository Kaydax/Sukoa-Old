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
  class MIDIPatternInteractionMouseShiftDown : MIDIPatternInteraction
  {
    Vector2d ClickLocation { get; }
    SelectedSNote? ClickedNote { get; }

    public MIDIPatternInteractionMouseShiftDown(MIDIPatternConnect pianoRollPattern) : base(pianoRollPattern)
    {
      ClickLocation = GetMousePos();
      ClickedNote = PianoRollPattern.GetNoteAtLocation(ClickLocation);
      if(ClickedNote != null)
      {
        var clickedNote = ClickedNote.Value;
        if(PianoRollPattern.IsNoteSelected(clickedNote))
        {
          PianoRollPattern.DeselectNote(clickedNote);
        }
        else
        {
          PianoRollPattern.SelectNote(clickedNote);
        }
      }
      ContinueWith(null);
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
        return new MIDIPatternInteractionSelectionRectangle(PianoRollPattern, ClickLocation);
      }
      return null;
    }
  }
}
