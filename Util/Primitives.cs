using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.Util
{
  public struct Rectangle
  {
    public Rectangle(float top, float right, float bottom, float left)
    {
      Top = top;
      Right = right;
      Bottom = bottom;
      Left = left;
    }
    
    public Rectangle(Vector2 point1, Vector2 point2, bool normalize = true) : this(point1.Y, point2.X, point2.Y, point1.X)
    {
      if(normalize)
      {
        Normalize();
      }
    }

    public float Top { get; set; }
    public float Right { get; set; }
    public float Bottom { get; set; }
    public float Left { get; set; }

    public Vector2 TopLeft => new Vector2(Top, Left);
    public Vector2 TopRight => new Vector2(Top, Right);
    public Vector2 BottomLeft => new Vector2(Bottom, Left);
    public Vector2 BottomRight => new Vector2(Bottom, Right);

    public bool IsValid => Top < Bottom && Left < Right;

    public Rectangle GetNormalized()
    {
      // Can clone like this because it's a struct
      var cloned = this;

      cloned.Normalize();
      return cloned;
    }

    public void Normalize()
    {
      float temp;
      if(Top > Bottom)
      {
        temp = Top;
        Top = Bottom;
        Bottom = temp;
      }
      if(Left > Right)
      {
        temp = Left;
        Left = Right;
        Right = temp;
      }
    }

    public Rectangle OffsetBy(Vector2 vec)
    {
      var cloned = this;

      cloned.Top += vec.Y;
      cloned.Bottom += vec.Y;
      cloned.Left += vec.X;
      cloned.Right += vec.X;
      return cloned;
    }

    public Rectangle CombineBoundsWith(Rectangle rect)
    {
      var cloned = this;
      
      cloned.Top = Math.Min(cloned.Top, rect.Top);
      cloned.Bottom = Math.Max(cloned.Bottom, rect.Bottom);
      cloned.Left = Math.Min(cloned.Left, rect.Left);
      cloned.Right = Math.Max(cloned.Right, rect.Right);
      return cloned;
    }
  }
}
