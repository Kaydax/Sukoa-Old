using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.Util
{
  public class SmoothZoomView
  {
    VelocityEase topEase = new VelocityEase(0);
    VelocityEase bottomEase = new VelocityEase(0);
    VelocityEase leftEase = new VelocityEase(0);
    VelocityEase rightEase = new VelocityEase(0);

    double topSavedValue = 0;
    double bottomSavedValue = 0;
    double leftSavedValue = 0;
    double rightSavedValue = 0;

    public double EaseTop => topSavedValue;
    public double EaseBottom => bottomSavedValue;
    public double EaseLeft => leftSavedValue;
    public double EaseRight => rightSavedValue;

    public double EaseWidth => EaseRight - EaseLeft;
    public double EaseHeight => EaseBottom - EaseTop;

    public double TrueTop => topEase.End;
    public double TrueBottom => bottomEase.End;
    public double TrueLeft => leftEase.End;
    public double TrueRight => rightEase.End;

    public double TrueWidth => TrueRight - TrueLeft;
    public double TrueHeight => TrueBottom - TrueTop;

    public double MaxLeft { get; set; } = double.NaN;
    public double MaxRight { get; set; } = double.NaN;
    public double MaxTop { get; set; } = double.NaN;
    public double MaxBottom { get; set; } = double.NaN;

    public double MinWidth { get; set; } = double.NaN;
    public double MinHeight { get; set; } = double.NaN;
    public double MaxWidth { get; set; } = double.NaN;
    public double MaxHeight { get; set; } = double.NaN;

    public SmoothZoomView(double top, double bottom, double left, double right)
    {
      topEase.ForceValue(top);
      bottomEase.ForceValue(bottom);
      leftEase.ForceValue(left);
      rightEase.ForceValue(right);

      foreach(var ease in new[] { topEase, bottomEase, leftEase, rightEase, })
      {
        ease.Duration = 0.1;
        ease.Slope = 2;
        ease.VelocityPower = 2;
      }
    }

    double TryClamp(double val, double from, double to)
    {
      if(!double.IsNaN(from) && val < from) val = from;
      if(!double.IsNaN(to) && val > to) val = to;
      return val;
    }

    void TryClampRange(ref double start, ref double end, double from, double to, double minSize, double maxSize)
    {
      var center = (start + end) / 2;
      var width = end - start;

      if(!double.IsNaN(minSize) && width < minSize)
      {
        start = center - minSize / 2;
        end = center + minSize / 2;
        width = minSize;
      }
      if(!double.IsNaN(maxSize) && width > maxSize)
      {
        start = center - maxSize / 2;
        end = center + maxSize / 2;
        width = maxSize;
      }

      if(!double.IsNaN(from) && start < from)
      {
        start = from;
        end = from + width;
      }
      if(!double.IsNaN(to) && end > to)
      {
        end = to;
        start = to - width;
      }
      if(!double.IsNaN(from) && start < from)
      {
        start = from;
      }
    }

    void ZoomRangeAt(ref double start, ref double end, double at, double zoom)
    {
      double length = end - start;
      double pos = start + length * at;
      start -= pos;
      end -= pos;
      start *= zoom;
      end *= zoom;
      start += pos;
      end += pos;
    }

    public void Update()
    {
      topSavedValue = TryClamp(topEase.GetValue(), MaxTop, MaxBottom);
      bottomSavedValue = TryClamp(bottomEase.GetValue(), MaxTop, MaxBottom);
      leftSavedValue = TryClamp(leftEase.GetValue(), MaxLeft, MaxRight);
      rightSavedValue = TryClamp(rightEase.GetValue(), MaxLeft, MaxRight);
    }

    public void SetTop(double val) => topEase.SetEnd(TryClamp(val, MaxTop, MaxBottom));
    public void SetBottom(double val) => bottomEase.SetEnd(TryClamp(val, MaxTop, MaxBottom));
    public void SetLeft(double val) => leftEase.SetEnd(TryClamp(val, MaxLeft, MaxRight));
    public void SetRight(double val) => rightEase.SetEnd(TryClamp(val, MaxLeft, MaxRight));

    public void ZoomHorizontalAt(double at, double zoom)
    {
      var left = TrueLeft;
      var right = TrueRight;
      ZoomRangeAt(ref left, ref right, at, zoom);
      TryClampRange(ref left, ref right, MaxLeft, MaxRight, MinWidth, MaxWidth);
      leftEase.SetEnd(left);
      rightEase.SetEnd(right);
    }

    public void ZoomVerticalAt(double at, double zoom)
    {
      var top = TrueTop;
      var bottom = TrueBottom;
      ZoomRangeAt(ref top, ref bottom, at, zoom);
      TryClampRange(ref top, ref bottom, MaxTop, MaxBottom, MinHeight, MaxHeight);
      topEase.SetEnd(top);
      bottomEase.SetEnd(bottom);
    }

    public void ScrollHorizontalBy(double by)
    {
      var left = TrueLeft;
      var right = TrueRight;
      left += by;
      right += by;
      TryClampRange(ref left, ref right, MaxLeft, MaxRight, MinWidth, MinHeight);
      leftEase.SetEnd(left);
      rightEase.SetEnd(right);
    }

    public void ScrollVerticalBy(double by)
    {
      var top = TrueTop;
      var bottom = TrueBottom;
      top += by;
      bottom += by;
      TryClampRange(ref top, ref bottom, MaxTop, MaxBottom, MinWidth, MinHeight);
      topEase.SetEnd(top);
      bottomEase.SetEnd(bottom);
    }

    public double TransformXToInside(double x)
    {
      x *= EaseWidth;
      x += EaseLeft;
      return x;
    }

    public double TransformYToInside(double y)
    {
      y *= EaseHeight;
      y += EaseTop;
      return y;
    }

    public double TransformXToOutside(double x)
    {
      x -= EaseLeft;
      x /= EaseWidth;
      return x;
    }

    public double TransformYToOutside(double y)
    {
      y -= EaseTop;
      y /= EaseHeight;
      return y;
    }
  }
}
