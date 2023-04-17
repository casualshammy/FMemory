using FMemory.Common.Interfaces;
using System;

namespace FMemory.PatternHelpers
{
    /// <summary>
    ///     This modifier just adds index of first "??" statement in pattern
    /// </summary>
    internal class AddModifier : IPatternModifier
    {
        public uint Offset { get; private set; }

        public AddModifier(uint val)
        {
            Offset = val;
        }

        public IntPtr Apply(IMemoryManager bm, IntPtr addr)
        {
            return addr + (int)Offset;
        }
    }
}
