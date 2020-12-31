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
  class PianoRollInteractionMouseDown : PianoRollInteraction
  {
    Vector2d ClickLocation { get; }
    SelectedSNote? ClickedNote { get; }

    public PianoRollInteractionMouseDown(PianoRollPattern pianoRollPattern) : base(pianoRollPattern)
    {
      if(ImGui.GetIO().KeyShift)
      {
        ContinueWith(new PianoRollInteractionMouseShiftDown(PianoRollPattern));
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
        return new PianoRollInteractionIdle(PianoRollPattern);
      }
      if(ClickLocation != GetMousePos())
      {
        if(ClickedNote != null)
        {
          return new PianoRollInteractionMoveSelectedNotes(PianoRollPattern, ClickLocation);
        }
        else
        {
          return new PianoRollInteractionSelectionRectangle(PianoRollPattern, ClickLocation);
        }
      }
      return null;
    }
  }
}
