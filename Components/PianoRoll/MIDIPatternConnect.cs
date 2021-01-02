using Sukoa.MIDI;
using Sukoa.UI;
using Sukoa.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.Components.PianoRoll
{

  public partial class MIDIPatternConnect
  {
    public MIDIPattern Pattern { get; }

    public double NoteSnapInterval { get; set; } = 1;

    public SmoothZoomView ViewFrame { get; } = new SmoothZoomView(0, 128, 0, 100)
    {
      MaxBottom = new UIProperty<double>(() => Constants.KeyCount),
      MaxTop = 0,
      MaxLeft = 0,
    };

    public MIDIPatternConnect(MIDIPattern pattern)
    {
      Pattern = pattern;
    }

    public void Update()
    {
      ViewFrame.Update();
      SelectionOffsetPosEase.Update();
    }
  }
}
