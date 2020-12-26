using ImGuiNET;
using Sukoa.MIDI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.Components
{
  // Action state machine
  interface IPianoRollAction
  {
    // NextAction == this ? Keep this action
    // NextAction != this ? Start next action
    // NextAction == null ? End action sequence
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
      NextAction = this;
      PianoRollPattern = pianoRollPattern;
    }

    public PianoRollPattern PianoRollPattern { get; }

    public IPianoRollAction NextAction { get; private set; }


    public void Act()
    {
      if(NextAction != null) throw new Exception("Can't act on an action that has been ended");
      NextAction = DoAction();
    }

    protected void ContinueWith(IPianoRollAction action)
    {
      NextAction = action;
    }

    public abstract IPianoRollAction DoAction();

    void IPianoRollAction.Act()
    {
      
    }

    protected Vector2 GetMousePos()
    {
      var pixelPos = ImGui.GetMousePos() - ImGui.GetWindowPos() - ImGui.GetCursorStartPos();
      var relativePos = pixelPos / ImGui.GetItemRectSize();
      return PianoRollPattern.GetPositionInside(relativePos);
    }
  }

  class PianoRollActionMouseDown : PianoRollAction
  {
    Vector2 ClickLocation { get; }
    SNote ClickedNote { get; }

    public PianoRollActionMouseDown(PianoRollPattern pianoRollPattern) : base(pianoRollPattern)
    {
      if(ImGui.GetIO().KeyShift)
      {
        ContinueWith(new PianoRollActionMouseShiftDown(PianoRollPattern));
        return;
      }
      ClickLocation = GetMousePos();

      PianoRollPattern.DeselectAllNotes();

      ClickedNote = PianoRollPattern.GetNoteAtLocation(ClickLocation);
      if(ClickedNote != null)
      {
        PianoRollPattern.SelectNote(ClickedNote);
      }

      ContinueWith(null);
      return;
    }

    public override IPianoRollAction DoAction()
    {
      throw new NotImplementedException();
    }
  }

  class PianoRollActionMouseShiftDown : PianoRollAction
  {
    Vector2 ClickLocation { get; }
    SNote ClickedNote { get; }

    public PianoRollActionMouseShiftDown(PianoRollPattern pianoRollPattern) : base(pianoRollPattern)
    {
      ClickLocation = GetMousePos();
      ClickedNote = PianoRollPattern.GetNoteAtLocation(ClickLocation);
      if(ClickedNote != null)
      {
        if(PianoRollPattern.SelectedNotes.Contains(ClickedNote))
        {
          PianoRollPattern.DeselectNote(ClickedNote);
        }
        else
        {
          PianoRollPattern.SelectNote(ClickedNote);
        }
      }
      ContinueWith(null);
    }

    public override IPianoRollAction DoAction()
    {
      throw new NotImplementedException();
    }
  }
}
