using System;

namespace FMemory.PatternHelpers
{
    public interface IModifier
    {
        IntPtr Apply(MemoryManager bm, IntPtr address);
    }
}
