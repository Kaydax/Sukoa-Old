using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace Sukoa.Renderer
{
  public interface IRenderTarget
  {
    public Framebuffer FrameBuffer { get; }
  }
}
