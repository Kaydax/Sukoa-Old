using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.Components.PianoRoll.Interactions
{
  class PianoRollInteractionMoveSelectedNotes : PianoRollInteraction
  {
    Vector2 StartLocation { get; }

    public PianoRollInteractionMoveSelectedNotes(PianoRollPattern pianoRollPattern, Vector2 startLocation) : base(pianoRollPattern)
    {
      StartLocation = startLocation;
    }

    public override IPianoRollInteraction DoInteraction()
    {
      base.DoInteraction();

      var pos = GetMousePos();
      PianoRollPattern.SetSelectionPosOffset(StartLocation, pos - StartLocation);

      if(!ImGui.IsMouseDown(ImGuiMouseButton.Left))
      {
        PianoRollPattern.ApplySelectionPosOffset();

        return new PianoRollInteractionIdle(PianoRollPattern);
      }
      return null;
    }
  }
}
