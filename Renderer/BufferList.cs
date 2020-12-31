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
    DeviceBuffer IndexBuffer { get; } = null;

    int bufferPos = 0;

    int[] indices;
    int indicesPerShape;
    int vertsPerShape;

    T[] buffer;
    int itemSizeInBytes;

    public BufferList(GraphicsDevice gd, int bufferSize, int[] indices = null)
    {
      itemSizeInBytes = Unsafe.SizeOf<T>();
      GraphicsDevice = gd;
      buffer = new T[bufferSize];

      if(indices != null)
      {
        if(indices.Length == 0) throw new Exception("Indices must be longer than zero");
        if(indices.Min() != 0) throw new Exception("Smallest index must be zero");
        var max = indices.Max();
        if(max < 0) throw new Exception("Biggest index can't be smaller than 0");
        vertsPerShape = max + 1;
        indicesPerShape = indices.Length;

        if(bufferSize % vertsPerShape != 0) throw new Exception("Buffer size must be a multiple of shape vertex count");

        var indexArray = new int[bufferSize / vertsPerShape * indicesPerShape];
        for(int i = 0; i < bufferSize / vertsPerShape; i++)
        {
          for(int j = 0; j < indicesPerShape; j++)
          {
            indexArray[i * indicesPerShape + j] = i * vertsPerShape + indices[j];
          }
        }

        this.indices = indexArray;

        IndexBuffer = dispose.Add(Factory.CreateBuffer(new BufferDescription((uint)(indexArray.Length * sizeof(int)), BufferUsage.IndexBuffer)));
        GraphicsDevice.UpdateBuffer(IndexBuffer, 0, indexArray);
      }
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
        bufferCount--;
        posInBuffer = buffer.Length;
      }

      if(bufferCount == 0)
      {
        return;
      }

      while(VertexBufferCache.Count < bufferCount)
      {
        var newBuffer = Factory.CreateBuffer(new BufferDescription((uint)(buffer.Length * itemSizeInBytes), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        VertexBufferCache.Add(newBuffer);
      }
      var bufferToWrite = VertexBufferCache[bufferCount - 1];
      lock (GraphicsDevice)
        GraphicsDevice.UpdateBuffer(bufferToWrite, 0, ref buffer[0], (uint)(posInBuffer * itemSizeInBytes));
      cl.SetVertexBuffer(0, bufferToWrite);

      if(indices == null)
      {
        cl.Draw((uint)posInBuffer);
      }
      else
      {
        cl.SetIndexBuffer(IndexBuffer, IndexFormat.UInt32);
        if(posInBuffer % vertsPerShape != 0) throw new Exception("Submitted an incomplete shape to the buffer flushing");
        cl.DrawIndexed((uint)(posInBuffer / vertsPerShape * indicesPerShape));
      }
    }

    public void Dispose()
    {
      dispose.Dispose();
    }
  }
}
