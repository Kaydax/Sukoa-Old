using Sukoa.MIDI;
using Sukoa.Util;
using Sukoa.Components.PianoRoll.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.Components.PianoRoll
{
  public partial class PianoRollPattern
  {
    class SelectionDragOffset
    {
      VelocityEase YOffsetEase = new VelocityEase(0) { Duration = 0.05, Slope = 1, VelocityPower = 1 };
      VelocityEase XOffsetEase = new VelocityEase(0) { Duration = 0.0, Slope = 2, VelocityPower = 2 };

      public Vector2d EaseOffset { get; private set; } = new Vector2d();
      public Vector2d TrueOffset => new Vector2d(XOffsetEase.End, YOffsetEase.End);

      public void Update()
      {
        EaseOffset = new Vector2d(XOffsetEase.GetValue(), YOffsetEase.GetValue());
      }

      public void SetEnd(Vector2d end)
      {
        XOffsetEase.SetEnd(end.X);
        YOffsetEase.SetEnd(end.Y);
      }

      public void ForceReset()
      {
        XOffsetEase.ForceValue(0);
        YOffsetEase.ForceValue(0);
      }
    }

    public Vector2d SelectionPosOffset => SelectionOffsetPosEase.EaseOffset;
    SelectionDragOffset SelectionOffsetPosEase { get; } = new SelectionDragOffset();

    public void SetSelectionPosOffset(Vector2d start, Vector2d offset)
    {
      var yOffset = Math.Round(Math.Floor(offset.Y + start.Y) - Math.Floor(start.Y));
      var xOffset = offset.X;

      if(SelectedNotesBounds.HasValue)
      {
        var dist = (SelectedNotesBounds.Value.Left + xOffset) % NoteSnapInterval;
        xOffset -= dist;
      }


      if(SelectedNotesBounds.HasValue)
      {
        var bounds = SelectedNotesBounds.Value;
        if(bounds.Left + xOffset < 0)
        {
          xOffset = -bounds.Left;
        }
        if(bounds.Top + yOffset < 0)
        {
          yOffset = -bounds.Top;
        }
        if(bounds.Bottom + yOffset > 128)
        {
          yOffset = 128 - bounds.Bottom;
        }
      }


      SelectionOffsetPosEase.SetEnd(new Vector2d(xOffset, yOffset));
    }

    public void ClearSelectionPosOffset()
    {
      SelectionOffsetPosEase.ForceReset();
    }

    public void ApplySelectionPosOffset()
    {
      double xOffset = SelectionOffsetPosEase.TrueOffset.X;
      int keyOffset = (int)Math.Round(SelectionOffsetPosEase.TrueOffset.Y);

      var action = new PianoRollActionMoveNotes(this, SelectedNotes, xOffset, keyOffset);
      action.Apply();

      ClearSelectionPosOffset();
    }
  }
}
