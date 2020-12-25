using System;
using System.Collections.Generic;
using System.Linq;

using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace Sukoa.Renderer
{
  public class BufferList<T> : IDisposable where T : unmanaged
  {
    GraphicsDevice GraphicsDevice { get; }
    ResourceFactory Factory => GraphicsDevice.ResourceFactory;

    DisposeGroup dispose = new DisposeGroup();

    List<DeviceBuffer> VertexBufferCache { get; } = new List<DeviceBuffer>();

    int bufferPos = 0;

    T[] buffer;
    int itemSizeInBytes;

    public BufferList(GraphicsDevice gd, int bufferSize)
    {
      itemSizeInBytes = Unsafe.SizeOf<T>();
      GraphicsDevice = gd;
      buffer = new T[bufferSize];
    }

    public void Reset()
    {
      bufferPos = 0;
    }

    public void Push(CommandList cl, T vert)
    {
      buffer[bufferPos++ % buffer.Length] = vert;
      if(bufferPos % buffer.Length == 0)
      {
        Flush(cl);
      }
    }

    public void Flush(CommandList cl)
    {
      var posInBuffer = bufferPos % buffer.Length;
      var bufferCount = (bufferPos - posInBuffer) / buffer.Length + 1;
      if(posInBuffer == 0)
      {
        // bufferCount--;
        posInBuffer = buffer.Length;
      }

      if(bufferCount == 0){
        return;
      }

      while(VertexBufferCache.Count < bufferCount)
      {
        var newBuffer = Factory.CreateBuffer(new BufferDescription((uint)(buffer.Length * itemSizeInBytes), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        VertexBufferCache.Add(newBuffer);
      }
      var bufferToWrite = VertexBufferCache[bufferCount - 1];
      GraphicsDevice.UpdateBuffer(bufferToWrite, 0, ref buffer[0], (uint)(posInBuffer * itemSizeInBytes));
      cl.SetVertexBuffer(0, bufferToWrite);
      cl.Draw((uint)posInBuffer);
    }

    public void Dispose()
    {
      dispose.Dispose();
    }
  }
}
