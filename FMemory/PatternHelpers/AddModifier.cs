using System;

namespace FMemory.PatternHelpers
{
    /// <summary>
    ///     This modifier just adds index of first "??" statement in pattern
    /// </summary>
    internal class AddModifier : IModifier
    {
        public uint Offset { get; private set; }

        public AddModifier(uint val)
        {
            Offset = val;
        }

        public IntPtr Apply(MemoryManager bm, IntPtr addr)
        {
            return addr + (int)Offset;
        }
    }
}
