﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.MIDI
{
  public class SNote
  {
    double length;

    public double Start { get; set; }
    public byte Velocity { get; set; }

    public double End
    {
      get => Start + length;
      set
      {
        Length = value - Start;
      }
    }

    public double Length
    {
      get => length;
      set
      {
        if(value < -0.00000001)
          throw new ArgumentException("Note can not have a negative length");
        length = value;
      }
    }

    public void SetStartOnly(double newStart)
    {
      double newLength = End - newStart;
      Start = newStart;
      Length = newLength;
    }

    public SNote(double start, double end, byte vel)
    {
      this.Start = start;
      this.Length = end - start;
      this.Velocity = vel;
    }

    public virtual SNote Clone()
    {
      return new SNote(Start, End, Velocity);
    }
  }
}
