using System.Runtime.InteropServices;

namespace FMemory.WinAPI
{
    [StructLayout(LayoutKind.Sequential)]
    public struct _PSAPI_WORKING_SET_EX_BLOCK
    {
        public ulong Flags;

        public ulong Valid;
    }
}
