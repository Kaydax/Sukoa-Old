using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.Util
{
  public static class Extensions
  {
    public static Vector2d ToDoubleVec(this Vector2 vec)
    {
      return new Vector2d(vec.X, vec.Y);
    }

    public static Vector2 ToFloatVec(this Vector2d vec)
    {
      return new Vector2((float)vec.X, (float)vec.Y);
    }
  }
}
