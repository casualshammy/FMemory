﻿using System;

namespace FMemory.Interfaces.Data
{
    [Flags]
    public enum MemoryFreeType
    {
        MEM_DECOMMIT = 0x4000,
        MEM_RELEASE = 0x8000
    }

}