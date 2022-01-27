using FMemory.Interfaces;
using System;

namespace FMemory.PatternHelpers
{
    public interface IModifier
    {
        IntPtr Apply(IMemoryManager bm, IntPtr address);
    }
}
