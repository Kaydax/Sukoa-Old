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
    public Rectangle(double top, double right, double bottom, double left)
    {
      Top = top;
      Right = right;
      Bottom = bottom;
      Left = left;
    }
    
    public Rectangle(Vector2d point1, Vector2d point2, bool normalize = true) : this(point1.Y, point2.X, point2.Y, point1.X)
    {
      if(normalize)
      {
        Normalize();
      }
    }

    public double Top { get; set; }
    public double Right { get; set; }
    public double Bottom { get; set; }
    public double Left { get; set; }

    public Vector2d TopLeft => new Vector2d(Top, Left);
    public Vector2d TopRight => new Vector2d(Top, Right);
    public Vector2d BottomLeft => new Vector2d(Bottom, Left);
    public Vector2d BottomRight => new Vector2d(Bottom, Right);

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
      double temp;
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

    public Rectangle OffsetBy(Vector2d vec)
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
