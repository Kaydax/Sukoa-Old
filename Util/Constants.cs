using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.Util
{
  public static class Constants
  {
    public static readonly bool Use256Keys = false;
    public static readonly int KeyCount = Use256Keys ? 256 : 128;
  }
}
