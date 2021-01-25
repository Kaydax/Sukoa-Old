using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.Components.Common
{
  public abstract class UserAction : IUserAction
  {
    public bool Applied { get; private set; } = false;

    public void Apply()
    {
      if(Applied) throw new Exception("Can't apply an applied action again");
      ApplyInternal();
      Applied = true;
    }

    public void Undo()
    {
      if(!Applied) throw new Exception("Can't undo an unapplied action");
      UndoInternal();
      Applied = false;
    }

    protected abstract void ApplyInternal();
    protected abstract void UndoInternal();
  }
}
