﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FMemory.WinAPI
{
    [StructLayout(LayoutKind.Sequential)]
    public struct _PSAPI_WORKING_SET_EX_INFORMATION
    {
        public IntPtr VirtualAddress;

        public _PSAPI_WORKING_SET_EX_BLOCK VirtualAttributes;
    }
}