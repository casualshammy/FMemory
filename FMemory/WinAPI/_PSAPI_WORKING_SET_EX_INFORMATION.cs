using System;
using System.Runtime.InteropServices;

namespace FMemory.WinAPI
{
    [StructLayout(LayoutKind.Sequential)]
    public struct _PSAPI_WORKING_SET_EX_INFORMATION
    {
        public IntPtr VirtualAddress;

        public _PSAPI_WORKING_SET_EX_BLOCK VirtualAttributes;
    }
}
