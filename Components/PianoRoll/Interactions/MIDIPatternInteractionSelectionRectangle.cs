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
  class MIDIPatternInteractionMoveSelectedNotes : MIDIPatternInteraction
  {
    Vector2d StartLocation { get; }

    public MIDIPatternInteractionMoveSelectedNotes(MIDIPatternConnect pianoRollPattern, Vector2d startLocation) : base(pianoRollPattern)
    {
      StartLocation = startLocation;
    }

    public override IPianoRollInteraction? DoInteraction()
    {
      base.DoInteraction();

      var pos = GetMousePos();
      PianoRollPattern.SetSelectionPosOffset(StartLocation, pos - StartLocation);

      if(!ImGui.IsMouseDown(ImGuiMouseButton.Left))
      {
        PianoRollPattern.ApplySelectionPosOffset();

        return new MIDIPatternInteractionIdle(PianoRollPattern);
      }
      return null;
    }
  }
}
