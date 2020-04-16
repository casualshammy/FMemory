﻿using System;
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

        public IntPtr Apply(MemoryManager bm, IntPtr address)
        {
            switch (Type)
            {
                case LeaType.Byte:
                    return (IntPtr)bm.Read<byte>(address);

                case LeaType.Word:
                    return (IntPtr)bm.Read<ushort>(address);

                case LeaType.Dword:
                    return (IntPtr)bm.Read<uint>(address);

                case LeaType.E8:
                    // 4 = <call instruction size> - <E8>
                    return address + 4 + bm.Read<int>(address); 

                case LeaType.SimpleAddress:
                    return address;

                case LeaType.Cmp:
                    return address + 5 + bm.Read<int>(address);
                    
                case LeaType.RelativePlus8:
                    return address + 8 + bm.Read<int>(address);

                default:
                    throw new InvalidDataException("Unknown " + nameof(LeaType));
            }
        }
    }
}
