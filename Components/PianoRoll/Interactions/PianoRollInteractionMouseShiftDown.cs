using ImGuiNET;
using Sukoa.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.Components.PianoRoll.Interactions
{
  class PianoRollInteractionMouseShiftDown : PianoRollInteraction
  {
    Vector2d ClickLocation { get; }
    SelectedSNote? ClickedNote { get; }

    public PianoRollInteractionMouseShiftDown(PianoRollPattern pianoRollPattern) : base(pianoRollPattern)
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
        return new PianoRollInteractionIdle(PianoRollPattern);
      }
      if(ClickLocation != GetMousePos())
      {
        return new PianoRollInteractionSelectionRectangle(PianoRollPattern, ClickLocation);
      }
      return null;
    }
  }
}
