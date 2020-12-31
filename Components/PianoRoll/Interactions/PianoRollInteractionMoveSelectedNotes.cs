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
  class PianoRollInteractionSelectionRectangle : PianoRollInteraction
  {
    Vector2 StartLocation { get; }

    public PianoRollInteractionSelectionRectangle(PianoRollPattern pianoRollPattern, Vector2 startLocation) : base(pianoRollPattern)
    {
      StartLocation = startLocation;
    }

    public override IPianoRollInteraction DoInteraction()
    {
      base.DoInteraction();

      var pos = GetMousePos();
      var rect = new Rectangle(StartLocation, pos);
      PianoRollPattern.SetSelectionRectangle(rect);

      if(!ImGui.IsMouseDown(ImGuiMouseButton.Left))
      {
        PianoRollPattern.ClearSelectionRectangle();

        foreach(var n in PianoRollPattern.GetNotesInRectangle(rect))
        {
          PianoRollPattern.SelectNote(n);
        }

        return new PianoRollInteractionIdle(PianoRollPattern);
      }
      return null;
    }
  }
}
