using System;
using System.Collections.Generic;

namespace FMemory.Toolkit;

internal static class EnumerableExtensions
{
  public static int FirstIndexMatch<T>(
    this IEnumerable<T> _items,
    Func<T, bool> _matchCondition)
  {
    var index = 0;
    foreach (var item in _items)
    {
      if (_matchCondition.Invoke(item))
        return index;

      index++;
    }
    return -1;
  }
}
