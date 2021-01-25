using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.Components.PianoRoll.Interactions
{
  class MIDIPatternInteractionIdle : MIDIPatternInteraction
  {
    public MIDIPatternInteractionIdle(MIDIPatternConnect pianoRollPattern) : base(pianoRollPattern)
    {

    }

    public override IPianoRollInteraction? DoInteraction()
    {
      base.DoInteraction();
      if(ImGui.IsItemHovered())
      {
        if(ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
          return new MIDIPatternInteractionMouseDown(PianoRollPattern);
        }
      }
      return null;
    }
  }
}
