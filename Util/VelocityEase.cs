using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.Util
{
  public class VelocityEase
  {
    public double Duration { get; set; } = 1;
    public double Slope { get; set; } = 2;
    public double Supress { get; set; } = 1;

    public double Start { get; private set; }
    public double End { get; private set; }

    public bool Clamp { get; set; } = true;

    public double VelocityPower { get; set; } = 3;

    double v;
    DateTime start;

    double pow(double a, double b) => Math.Pow(a, b);

    double getRawInertiaPos(double t) => pow(t, VelocityPower) * (1 - t);
    double getRawInertiaVel(double t) => (1 - t) * pow(t, VelocityPower - 1) * VelocityPower - pow(t, VelocityPower);

    double getInertiaPos(double t) => getRawInertiaPos(1 - t) * v;
    double getInertiaVel(double t) => v - getRawInertiaVel(1 - t) * v;

    double getEasePos(double t) => pow(t, Slope) / (pow(1 - t, Slope) + pow(t, Slope));
    double getEaseVel(double t) =>
        (pow(-(-1 + t) * t, Slope - 1) * Slope) /
        pow(pow(1 - t, Slope) + pow(t, Slope), 2);

    public double GetValue()
    {
      double t = (DateTime.UtcNow - start).TotalSeconds / Duration;
      if(t > 1)
      {
        v = 0;
        return End;
      }

      double pos = getEasePos(t) * (End - Start) + getInertiaPos(t);
      pos += Start;

      if(Clamp)
      {
        if(Start < End)
        {
          if(pos < Start) pos = Start;
          if(pos > End) pos = End;
        }
        else
        {
          if(pos > Start) pos = Start;
          if(pos < End) pos = End;
        }
      }

      return pos;
    }

    public double GetValue(double min, double max)
    {
      var val = GetValue();
      if(val < min) val = min;
      if(val > max) val = max;
      return val;
    }

    public void SetEnd(double e)
    {
      double t = (DateTime.UtcNow - start).TotalSeconds / Duration;
      double vel;
      if(t > 1)
      {
        vel = 0;
        t = 1;
      }
      else
        vel = getEaseVel(t) * (End - Start) + getInertiaVel(t);
      vel /= Duration * Supress;
      if(vel < 0)
      { }
      double pos = getEasePos(t) * (End - Start) + Start + getInertiaPos(t);

      Start = pos;
      End = e;
      var scale = Math.Abs(Start - End);
      if(vel > scale) vel = scale;
      if(vel < -scale) vel = -scale;
      v = vel;
      start = DateTime.UtcNow;
    }

    public void ForceValue(double v)
    {
      Start = v;
      End = v;
      v = 0;
    }

    public VelocityEase(double initial)
    {
      Start = initial;
      End = initial;
      start = DateTime.UtcNow;
    }
  }
}
