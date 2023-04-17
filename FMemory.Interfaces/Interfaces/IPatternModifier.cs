using System;

namespace FMemory.Common.Interfaces;

public interface IPatternModifier
{
  IntPtr Apply(IMemoryManager _mm, IntPtr _address);
}
