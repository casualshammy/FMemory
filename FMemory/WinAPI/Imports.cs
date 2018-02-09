using System;
using System.Runtime.InteropServices;
using System.Security;

namespace FMemory.WinAPI
{
    internal static unsafe class Imports
    {
        /// <summary>
        ///     Frees the loaded dynamic-link library (DLL) module and, if necessary, decrements its reference count.
        ///     When the reference count reaches zero, the module is unloaded from the address space of the calling process and the
        ///     handle is no longer valid.
        /// </summary>
        /// <param name="handle">
        ///     A handle to the loaded library module.
        ///     The LoadLibrary, LoadLibraryEx, GetModuleHandle, or GetModuleHandleEx function returns this handle.
        /// </param>
        /// <returns>
        ///     If the function succeeds, the return value is nonzero.
        ///     If the function fails, the return value is zero
        /// </returns>
        /// <remarks>Created 2012-01-17 13:00 by Nesox.</remarks>
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

        /// <summary>
        ///     Opens an existing local process object.
        /// </summary>
        /// <param name="dwDesiredAccess">
        ///     The access to the process object. This access right is checked against the security descriptor for the process.
        ///     This parameter can be one or more of the process access rights.
        ///     If the caller has enabled the SeDebugPrivilege privilege, the requested access is granted regardless of the
        ///     contents of the security descriptor.
        /// </param>
        /// <param name="bInheritHandle">
        ///     if set to <c>true</c> processes created by this process will inherit the handle.
        ///     Otherwise, the processes do not inherit this handle.
        /// </param>
        /// <param name="dwProcessId">The identifier of the local process to be opened. </param>
        /// <returns>If the function succeeds, the return value is an open handle to the specified process.</returns>
        /// <remarks>Created 2012-01-17 13:00 by Nesox.</remarks>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern SafeMemoryHandle OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        /// <summary>
        ///     Loads the specified module into the address space of the calling process.
        ///     The specified module may cause other modules to be loaded.
        /// </summary>
        /// <param name="libraryName">
        ///     The name of the module.
        ///     This can be either a library module (a .dll file) or an executable module (an .exe file).
        ///     The name specified is the file name of the module and is not related to the name stored in the library module
        ///     itself, as specified by the LIBRARY keyword in the module-definition (.def) file.
        ///     If the string specifies a full path, the function searches only that path for the module.
        ///     If the string specifies a relative path or a module name without a path, the function uses a standard search
        ///     strategy to find the module;
        ///     If the function cannot find the module, the function fails. When specifying a path, be sure to use backslashes (\),
        ///     not forward slashes (/).
        ///     If the string specifies a module name without a path and the file name extension is omitted, the function appends
        ///     the default library extension .dll to the module name.
        ///     To prevent the function from appending .dll to the module name, include a trailing point character (.) in the
        ///     module name string.
        /// </param>
        /// <returns>
        ///     If the function succeeds, the return value is a handle to the module.
        ///     If the function fails, the return value is 0
        /// </returns>
        /// <remarks>Created 2012-01-17 13:00 by Nesox.</remarks>
        [DllImport("kernel32")]
        [SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr LoadLibrary(string libraryName);

        /// <summary>
        ///     Opens an existing thread object.
        /// </summary>
        /// <param name="dwDesiredAccess">
        ///     The access to the thread object.
        ///     This access right is checked against the security descriptor for the thread. This parameter can be one or more of
        ///     the thread access rights.
        ///     If the caller has enabled the SeDebugPrivilege privilege, the requested access is granted regardless of the
        ///     contents of the security descriptor.
        /// </param>
        /// <param name="bInheritHandle">
        ///     if set to <c>true</c> processes created by this process will inherit the handle.
        ///     Otherwise, the processes do not inherit this handle.
        /// </param>
        /// <param name="dwThreadId">The identifier of the thread to be opened.</param>
        /// <returns>
        ///     If the function succeeds, the return value is an open handle to the specified thread.
        ///     If the function fails, the return value is <c>IntPtr.Zero</c>
        /// </returns>
        /// <remarks>Created 2012-02-15</remarks>
        [DllImport("kernel32", EntryPoint = "OpenThread", SetLastError = true)]
        internal static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        /// <summary>
        ///     Reserves or commits a region of memory within the virtual address space of a specified process.
        ///     The function initializes the memory it allocates to zero, unless MEM_RESET is used.
        /// </summary>
        /// <param name="hProcess">
        ///     The handle to a process. The function allocates memory within the virtual address space of this process.
        ///     The handle must have the PROCESS_VM_OPERATION access right.
        /// </param>
        /// <param name="dwAddress">
        ///     The pointer that specifies a desired starting address for the region of pages that you want to allocate.
        ///     If you are reserving memory, the function rounds this address down to the nearest multiple of the allocation
        ///     granularity.
        ///     If you are committing memory that is already reserved, the function rounds this address down to the nearest page
        ///     boundary. To determine the size of a page and the allocation granularity on the host computer, use the
        ///     GetSystemInfo function.
        ///     If lpAddress is <c>IntPtr.Zero</c>, the function determines where to allocate the region.
        /// </param>
        /// <param name="nSize">
        ///     The size of the region of memory to allocate, in bytes.
        ///     If lpAddress is <c>IntPtr.Zero</c>, the function rounds dwSize up to the next page boundary.
        ///     If lpAddress is not <c>IntPtr.Zero</c>, the function allocates all pages that contain one or more bytes in the
        ///     range from lpAddress to lpAddress+dwSize. This means, for example, that a 2-byte range that straddles a page
        ///     boundary causes the function to allocate both pages.
        /// </param>
        /// <param name="dwAllocationType">The type of memory allocation. </param>
        /// <param name="dwProtect">
        ///     The memory protection for the region of pages to be allocated.
        ///     If the pages are being committed, you can specify any one of the memory protection constants.
        /// </param>
        /// <returns>
        ///     If the function succeeds, the return value is the base address of the allocated region of pages.
        ///     If the function fails, the return value is <c>IntPtr.Zero</c>
        /// </returns>
        /// <remarks>Created 2012-02-15</remarks>
        [DllImport("kernel32", EntryPoint = "VirtualAllocEx")]
        internal static extern IntPtr VirtualAllocEx(SafeMemoryHandle hProcess, IntPtr dwAddress, int nSize, MemoryAllocationType dwAllocationType, MemoryProtectionType dwProtect);

        /// <summary>
        ///     Releases, decommits, or releases and decommits a region of memory within the virtual address space of a specified
        ///     process.
        /// </summary>
        /// <param name="hProcess">
        ///     A handle to a process. The function frees memory within the virtual address space of the process.
        ///     The handle must have the PROCESS_VM_OPERATION access right.
        /// </param>
        /// <param name="dwAddress">
        ///     A pointer to the starting address of the region of memory to be freed.
        ///     If the dwFreeType parameter is MEM_RELEASE, lpAddress must be the base address returned by the VirtualAllocEx
        ///     function when the region is reserved.
        /// </param>
        /// <param name="nSize">
        ///     The size of the region of memory to free, in bytes.
        ///     If the dwFreeType parameter is MEM_RELEASE, dwSize must be 0 (zero). The function frees the entire region that is
        ///     reserved in the initial allocation call to VirtualAllocEx.
        ///     If dwFreeType is MEM_DECOMMIT, the function decommits all memory pages that contain one or more bytes in the range
        ///     from the lpAddress parameter to (lpAddress+dwSize). This means, for example, that a 2-byte region of memory that
        ///     straddles a page boundary causes both pages to be decommitted. If lpAddress is the base address returned by
        ///     VirtualAllocEx and dwSize is 0 (zero), the function decommits the entire region that is allocated by
        ///     VirtualAllocEx. After that, the entire region is in the reserved state.
        /// </param>
        /// <param name="dwFreeType">The type of free operation.</param>
        /// <returns>
        ///     If the function succeeds, the return value is a nonzero value.
        ///     If the function fails, the return value is 0 (zero). To get extended error information, call GetLastError.
        /// </returns>
        /// <remarks>
        ///     Each page of memory in a process virtual address space has a Page State. The VirtualFreeEx function can decommit a
        ///     range of pages that are in different states, some committed and some uncommitted. This means that you can decommit
        ///     a range of pages without first determining the current commitment state of each page. Decommitting a page releases
        ///     its physical storage, either in memory or in the paging file on disk.
        ///     If a page is decommitted but not released, its state changes to reserved. Subsequently, you can call VirtualAllocEx
        ///     to commit it, or VirtualFreeEx to release it. Attempting to read from or write to a reserved page results in an
        ///     access violation exception.
        ///     The VirtualFreeEx function can release a range of pages that are in different states, some reserved and some
        ///     committed. This means that you can release a range of pages without first determining the current commitment state
        ///     of each page. The entire range of pages originally reserved by VirtualAllocEx must be released at the same time.
        ///     If a page is released, its state changes to free, and it is available for subsequent allocation operations. After
        ///     memory is released or decommitted, you can never refer to the memory again. Any information that may have been in
        ///     that memory is gone forever. Attempts to read from or write to a free page results in an access violation
        ///     exception. If you need to keep information, do not decommit or free memory that contains the information.
        ///     The VirtualFreeEx function can be used on an AWE region of memory and it invalidates any physical page mappings in
        ///     the region when freeing the address space. However, the physical pages are not deleted, and the application can use
        ///     them. The application must explicitly call FreeUserPhysicalPages to free the physical pages. When the process is
        ///     terminated, all resources are automatically cleaned up.
        ///     Created 2012-02-15.
        /// </remarks>
        [DllImport("kernel32", EntryPoint = "VirtualFreeEx")]
        internal static extern bool VirtualFreeEx(SafeMemoryHandle hProcess, IntPtr dwAddress, int nSize, MemoryFreeType dwFreeType);
    }
}
