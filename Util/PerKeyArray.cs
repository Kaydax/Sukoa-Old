using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sukoa.Util
{
  // Used so that it's easier to create the array with inferred types
  // Just using PerKeyArray.Create()
  public abstract class PerKeyArray
  {
    public static PerKeyArray<T> Create<T>(Func<T> initializer)
    {
      return new PerKeyArray<T>(initializer);
    }
  }

  public class PerKeyArray<T> : PerKeyArray, IEnumerable<T>, ICloneable
  {
    T[] Items { get; }

    public PerKeyArray(Func<T> initializer) : this(SUtil.CreatePerKeyItemArray(initializer))
    { }

    private PerKeyArray(T[] items)
    {
      Items = items;
    }

    public T this[int idx]
    {
      get => Items[idx];
      set => Items[idx] = value;
    }

    public int Length => Items.Length;

    public IEnumerator<T> GetEnumerator()
    {
      return ((IEnumerable<T>)Items).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return Items.GetEnumerator();
    }

    public object Clone()
    {
      return new PerKeyArray<T>(Items.ToArray());
    }

    // Creates a new PerKeyArray, mapping each element using the function
    public PerKeyArray<E> Map<E>(Func<T, E> mapper)
    {
      return new PerKeyArray<E>(Items.Select(mapper).ToArray());
    }

    // Same as above, except parallelized
    public PerKeyArray<E> MapParallel<E>(Func<T, E> mapper)
    {
      var items = new E[Items.Length];
      Parallel.For(0, items.Length, i =>
      {
        items[i] = mapper(Items[i]);
      });
      return new PerKeyArray<E>(items);
    }

    // Creates a new PerKeyArray, shifting the items by the specified number of keys
    public PerKeyArray<T> Roll(int keys)
    {
      if(keys > 0)
      {
        return new PerKeyArray<T>(Items.Skip(Items.Length - keys).Concat(Items.Take(Items.Length - keys)).ToArray());
      }
      else
      {
        return new PerKeyArray<T>(Items.Skip(-keys).Concat(Items.Take(-keys)).ToArray());
      }
    }
  }
}
