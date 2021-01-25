using Sukoa.Components.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.Components.Project
{
  public class ProjectConnect
  {
    List<IUserAction> UndoQueue { get; } = new List<IUserAction>();
    List<IUserAction> RedoQueue { get; } = new List<IUserAction>();

    public void RunAction(IUserAction action)
    {
      UndoQueue.Add(action);
      action.Apply();
      if(RedoQueue.Count != 0) RedoQueue.Clear();
    }

    public bool Undo()
    {
      if(UndoQueue.Count == 0) return false;

      var action = UndoQueue[UndoQueue.Count - 1];
      UndoQueue.RemoveAt(UndoQueue.Count - 1);

      action.Undo();

      RedoQueue.Add(action);
     
      return true;
    }

    public bool Redo()
    {

      if(RedoQueue.Count == 0) return false;

      var action = RedoQueue[RedoQueue.Count - 1];
      RedoQueue.RemoveAt(RedoQueue.Count - 1);

      action.Apply();

      UndoQueue.Add(action);

      return true;
    }
  }
}
