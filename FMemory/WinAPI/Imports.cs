using System;
using System.Runtime.InteropServices;
using System.Security;

namespace FMemory.WinAPI
{
    internal static unsafe class Imports
    {
        
        [DllImport("kernel32.dll")]
        [SuppressUnmanagedCodeSecurity]
        internal static extern bool FreeLibrary(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        internal static extern bool ReadProcessMemory(SafeMemoryHandle hProcess, IntPtr lpBaseAddress, byte* lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("Kernel32.dll", EntryPoint = "RtlMoveMemory", SetLastError = false)]
        internal static extern void MoveMemory(void* dest, void* src, int size);

        [DllImport("kernel32.dll")]
        [SuppressUnmanagedCodeSecurity]
        internal static extern bool VirtualProtectEx(SafeMemoryHandle hProcess, IntPtr lpAddress, IntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        internal static extern bool WriteProcessMemory(SafeMemoryHandle hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern SafeMemoryHandle OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);
        
        [DllImport("kernel32")]
        [SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr LoadLibrary(string libraryName);
        
        [DllImport("kernel32", EntryPoint = "OpenThread", SetLastError = true)]
        internal static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        
        [DllImport("kernel32", EntryPoint = "VirtualAllocEx")]
        internal static extern IntPtr VirtualAllocEx(SafeMemoryHandle hProcess, IntPtr dwAddress, int nSize, MemoryAllocationType dwAllocationType, MemoryProtectionType dwProtect);
        
        [DllImport("kernel32", EntryPoint = nameof(VirtualFreeEx))]
        internal static extern bool VirtualFreeEx(SafeMemoryHandle hProcess, IntPtr dwAddress, int nSize, MemoryFreeType dwFreeType);

        [DllImport("psapi", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool QueryWorkingSetEx(SafeMemoryHandle hProcess, [In, Out] _PSAPI_WORKING_SET_EX_INFORMATION[] pv, int cb);

    }
}
