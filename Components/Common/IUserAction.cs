using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.Components.Common
{
  public interface IUserAction
  {
    bool Applied { get; }

    // public long EstimatedRamUsage { get; }

    void Apply();
    void Undo();
  }
}
