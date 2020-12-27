using ImGuiNET;
using Sukoa.MIDI;
using Sukoa.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.Components
{
  #region Shared
  // Action state machine
  interface IPianoRollAction
  {
    // Next action to use. If NextAction is set, then this action is ended.
    bool IsEnded => NextAction != null;
    IPianoRollAction NextAction { get; }
    void Act();
  }



  interface IPianoRollPatternAction : IPianoRollAction
  {
    PianoRollPattern PianoRollPattern { get; }
  }



  abstract class PianoRollAction : IPianoRollPatternAction
  {
    protected PianoRollAction(PianoRollPattern pianoRollPattern)
    {
      PianoRollPattern = pianoRollPattern;
    }

    public PianoRollPattern PianoRollPattern { get; }

    public IPianoRollAction NextAction { get; private set; }


    public void Act()
    {
      var next = DoAction();
      if(next != null)
      {
        NextAction = next;
      }
    }

    protected void ContinueWith(IPianoRollAction action)
    {
      NextAction = action;
    }

    public virtual IPianoRollAction DoAction()
    {
      HandleZoomAction();
      return null;
    }

    protected void HandleZoomAction()
    {
      var io = ImGui.GetIO();
      var mouseRelativePos = GetMouseOutsidePos();

      if(ImGui.IsItemHovered())
      {
        if(io.MouseWheel != 0)
        {
          var zoom = Math.Pow(1.2, -io.MouseWheel);

          var viewFrame = PianoRollPattern.ViewFrame;

          if(io.KeyCtrl)
          {
            viewFrame.ZoomHorizontalAt(mouseRelativePos.X, zoom);
          }
          else if(io.KeyAlt)
          {
            viewFrame.ZoomVerticalAt(mouseRelativePos.Y, zoom);
          }
          else if(io.KeyShift)
          {
            viewFrame.ScrollHorizontalBy(viewFrame.TrueWidth / 8 * -io.MouseWheel);
          }
          else
          {
            viewFrame.ScrollVerticalBy(viewFrame.TrueHeight / 8 * -io.MouseWheel);
          }
        }
      }
    }

    protected Vector2 GetMouseOutsidePos()
    {
      var pixelPos = ImGui.GetMousePos() - ImGui.GetWindowPos() - ImGui.GetCursorStartPos();
      var relativePos = pixelPos / ImGui.GetItemRectSize();
      return relativePos;
    }

    protected Vector2 GetMousePos()
    {
      return PianoRollPattern.GetPositionInside(GetMouseOutsidePos());
    }
  }



  class PianoRollActionIdle : PianoRollAction
  {
    public PianoRollActionIdle(PianoRollPattern pianoRollPattern) : base(pianoRollPattern)
    {

    }

    public override IPianoRollAction DoAction()
    {
      base.DoAction();
      if(ImGui.IsItemHovered())
      {
        if(ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
          return new PianoRollActionMouseDown(PianoRollPattern);
        }
      }
      return null;
    }
  }
  #endregion



  class PianoRollActionMouseDown : PianoRollAction
  {
    Vector2 ClickLocation { get; }
    SelectedSNote? ClickedNote { get; }

    public PianoRollActionMouseDown(PianoRollPattern pianoRollPattern) : base(pianoRollPattern)
    {
      if(ImGui.GetIO().KeyShift)
      {
        ContinueWith(new PianoRollActionMouseShiftDown(PianoRollPattern));
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

    public override IPianoRollAction DoAction()
    {
      base.DoAction();
      if(!ImGui.IsMouseDown(ImGuiMouseButton.Left))
      {
        return new PianoRollActionIdle(PianoRollPattern);
      }
      if(ClickLocation != GetMousePos())
      {
        if(ClickedNote != null)
        {
          return new PianoRollActionMoveSelectedNotes(PianoRollPattern, ClickLocation);
        }
        else
        {
          return new PianoRollActionSelectionRectangle(PianoRollPattern, ClickLocation);
        }
      }
      return null;
    }
  }



  class PianoRollActionMouseShiftDown : PianoRollAction
  {
    Vector2 ClickLocation { get; }
    SelectedSNote? ClickedNote { get; }

    public PianoRollActionMouseShiftDown(PianoRollPattern pianoRollPattern) : base(pianoRollPattern)
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

    public override IPianoRollAction DoAction()
    {
      base.DoAction();
      if(!ImGui.IsMouseDown(ImGuiMouseButton.Left))
      {
        return new PianoRollActionIdle(PianoRollPattern);
      }
      if(ClickLocation != GetMousePos())
      {
        return new PianoRollActionSelectionRectangle(PianoRollPattern, ClickLocation);
      }
      return null;
    }
  }



  class PianoRollActionSelectionRectangle : PianoRollAction
  {
    Vector2 StartLocation { get; }

    public PianoRollActionSelectionRectangle(PianoRollPattern pianoRollPattern, Vector2 startLocation) : base(pianoRollPattern)
    {
      StartLocation = startLocation;
    }

    public override IPianoRollAction DoAction()
    {
      base.DoAction();

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

        return new PianoRollActionIdle(PianoRollPattern);
      }
      return null;
    }
  }



  class PianoRollActionMoveSelectedNotes : PianoRollAction
  {
    Vector2 StartLocation { get; }

    public PianoRollActionMoveSelectedNotes(PianoRollPattern pianoRollPattern, Vector2 startLocation) : base(pianoRollPattern)
    {
      StartLocation = startLocation;
    }

    public override IPianoRollAction DoAction()
    {
      base.DoAction();

      var pos = GetMousePos();
      PianoRollPattern.SetSelectionPosOffset(StartLocation, pos - StartLocation);

      if(!ImGui.IsMouseDown(ImGuiMouseButton.Left))
      {
        PianoRollPattern.ApplySelectionPosOffset();

        return new PianoRollActionIdle(PianoRollPattern);
      }
      return null;
    }
  }
}
