using FMemory.Common.Data;
using System.Collections.Generic;

namespace FMemory.Common.Interfaces;

public interface IMemoryPattern
{
  string Name { get; }
  IReadOnlyList<IPatternModifier> Modifiers { get; }

  IEnumerable<PatterSearchResult> Find(IMemoryManager _mm);
}