using FMemory.Interfaces;
using System;
using System.IO;

namespace FMemory.PatternHelpers
{
    /// <summary>
    ///     Modifies resulting address of pattern depending on cpu instruction
    /// </summary>
    public class LeaModifier : IModifier
    {
        public LeaType Type { get; private set; }

        public LeaModifier(LeaType type)
        {
            Type = type;
        }

        public IntPtr Apply(IMemoryManager memory, IntPtr address)
        {
            switch (Type)
            {
                case LeaType.Byte:
                    return (IntPtr)memory.Read<byte>(address);

                case LeaType.Word:
                    return (IntPtr)memory.Read<ushort>(address);

                case LeaType.Dword:
                    return (IntPtr)memory.Read<uint>(address);

                case LeaType.E8:
                    // 4 = <call instruction size> - <E8>
                    return address + 4 + memory.Read<int>(address); 

                case LeaType.SimpleAddress:
                    return address;

                case LeaType.Cmp:
                    return address + 5 + memory.Read<int>(address);
                    
                case LeaType.RelativePlus8:
                    return address + 8 + memory.Read<int>(address);

                default:
                    throw new InvalidDataException("Unknown " + nameof(LeaType));
            }
        }
    }
}
