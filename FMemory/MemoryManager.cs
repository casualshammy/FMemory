using FMemory.Helpers;
using Ax.Fw.Windows.WinAPI;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using FMemory.Interfaces;

namespace FMemory
{
    /// <summary>
    ///     Main class for memory interactions
    /// </summary>
    public sealed unsafe class MemoryManager : IDisposable, IMemoryManager
    {
        /// <summary>
        ///     Gets or sets the process handle
        /// </summary>
        public IntPtr ProcessHandle { get; private set; }

        /// <summary>
        ///     Gets the process.
        /// </summary>
        public Process Process { get; private set; }

        /// <summary>
        ///     Gets the address of image base.
        /// </summary>
        public IntPtr ImageBase { get; }

        /// <summary>
        ///     If set to true, every <see cref="ReadBytes"/> and <see cref="Read"/> will check if physical pages are backing allocation of memory
        /// </summary>
        public bool AvoidNotPhysicallyBackedTrapPages { get; set; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MemoryManager" /> class.
        /// </summary>
        /// <param name="proc">The process</param>
        public MemoryManager(Process proc)
        {
            if (proc.HasExited)
            {
                throw new AccessViolationException("Process: " + proc.Id + " has already exited. Can not attach to it.");
            }
            Process.EnterDebugMode();
            Process = proc;
            const ProcessAccessFlags a = ProcessAccessFlags.PROCESS_CREATE_THREAD |
                                         ProcessAccessFlags.PROCESS_QUERY_INFORMATION |
                                         ProcessAccessFlags.PROCESS_SET_INFORMATION | ProcessAccessFlags.PROCESS_TERMINATE |
                                         ProcessAccessFlags.PROCESS_VM_OPERATION | ProcessAccessFlags.PROCESS_VM_READ |
                                         ProcessAccessFlags.PROCESS_VM_WRITE | ProcessAccessFlags.SYNCHRONIZE;
            ProcessHandle = NativeMethods.OpenProcess(a, false, proc.Id);
            ImageBase = Process.MainModule.BaseAddress;
        }

        /// <summary>
        ///     Reads a specific number of bytes from memory
        /// </summary>
        /// <param name="address">The address in memory</param>
        /// <param name="count">The count of bytes to read</param>
        /// <returns>If method success, returns array of bytes. If not, returns empty array with zero size</returns>
        public byte[] ReadBytes(IntPtr address, int count)
        {
            if (count != 0)
            {
                // Yes, this address is valid, but... Well, if you really want it, just delete next instruction
                if (address == IntPtr.Zero)
                {
                    StackTrace stackTrace = new StackTrace(1, true);
                    throw new DetailedArgumentException("Address cannot be zero.", nameof(address), stackTrace.ToString());
                }
                if (AvoidNotPhysicallyBackedTrapPages)
                    ThrowIfMemoryIsNotPhysicallyBacked(address, count);
                byte[] buffer = new byte[count];
                fixed (byte* buf = buffer)
                    if (NativeMethods.ReadProcessMemory(ProcessHandle, address, buf, count, out int numRead) && numRead == count)
                        return buffer;
                int lastError = Marshal.GetLastWin32Error();
                throw new Win32Exception(lastError, $"Could not read bytes from 0x{address.ToString("X")}");
            }
            return new byte[0];
        }

        [HandleProcessCorruptedStateExceptions]
        private T InternalRead<T>(IntPtr address) where T : struct
        {
            try
            {
                if (address == IntPtr.Zero)
                    throw new InvalidOperationException("Cannot retrieve a value at address 0");

                object returnValue;
                switch (MarshalCache<T>.TypeCode)
                {
                    case TypeCode.Object:

                        if (MarshalCache<T>.RealType == typeof(IntPtr))
                            return (T)(object)*(IntPtr*)address;

                        if (!MarshalCache<T>.TypeRequiresMarshal)
                        {
                            T o = default(T);
                            void* ptr = MarshalCache<T>.GetUnsafePtr(ref o);
                            NativeMethods.MoveMemory(ptr, (void*)address, MarshalCache<T>.Size);
                            return o;
                        }
                        // All System.Object's require marshaling!
                        returnValue = Marshal.PtrToStructure(address, typeof(T));
                        break;
                    case TypeCode.SByte:
                        returnValue = *(sbyte*)address;
                        break;
                    case TypeCode.Byte:
                        returnValue = *(byte*)address;
                        break;
                    case TypeCode.Int16:
                        returnValue = *(short*)address;
                        break;
                    case TypeCode.UInt16:
                        returnValue = *(ushort*)address;
                        break;
                    case TypeCode.Int32:
                        returnValue = *(int*)address;
                        break;
                    case TypeCode.UInt32:
                        returnValue = *(uint*)address;
                        break;
                    case TypeCode.Int64:
                        returnValue = *(long*)address;
                        break;
                    case TypeCode.UInt64:
                        returnValue = *(ulong*)address;
                        break;
                    case TypeCode.Single:
                        returnValue = *(float*)address;
                        break;
                    case TypeCode.Double:
                        returnValue = *(double*)address;
                        break;
                    case TypeCode.Decimal:
                        returnValue = *(decimal*)address;
                        break;
                    case TypeCode.Boolean:
                        returnValue = *(byte*)address != 0;
                        break;
                    case TypeCode.Char:
                        returnValue = *(char*)address;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                return (T)returnValue;
            }
            catch (AccessViolationException)
            {
                return default(T);
            }
        }

        /// <summary>
        ///     Reads a value from the address in memory
        /// </summary>
        /// <param name="address">
        ///     The address to read
        /// </param>
        public T Read<T>(IntPtr address) where T : struct
        {
            fixed (byte* b = ReadBytes(address, MarshalCache<T>.Size))
                return InternalRead<T>((IntPtr)b);
        }

        /// <summary>
        ///     Writes a value to the address in memory
        /// </summary>
        /// <param name="address">
        ///     The address in memory
        /// </param>
        /// <param name="value">
        ///     The value to write
        /// </param>
        /// <returns>
        ///     True if it succeeds, false overwise
        /// </returns>
        public bool Write<T>(IntPtr address, T value)
        {
            byte[] buffer;
            IntPtr allocation = Marshal.AllocHGlobal(MarshalCache<T>.Size);
            try
            {
                Marshal.StructureToPtr(value, allocation, false);
                buffer = new byte[MarshalCache<T>.Size];
                Marshal.Copy(allocation, buffer, 0, MarshalCache<T>.Size);
            }
            finally
            {
                Marshal.FreeHGlobal(allocation);
            }

            // Fix the protection flags to EXECUTE_READWRITE!
            NativeMethods.VirtualProtectEx(ProcessHandle, address, (IntPtr)MarshalCache<T>.Size, PageProtection.PAGE_READWRITE, out uint oldProtect);
            bool returnValue = NativeMethods.WriteProcessMemory(ProcessHandle, address, buffer, MarshalCache<T>.Size, out int numWritten);
            NativeMethods.VirtualProtectEx(ProcessHandle, address, (IntPtr)MarshalCache<T>.Size, oldProtect, out oldProtect);

            return returnValue;
        }

        /// <summary>
        ///     Writes an array of bytes to memory.
        /// </summary>
        /// <param name="address">
        ///     The address to write to</param>
        /// <param name="bytes">
        ///     The byte array to write
        /// </param>
        /// <returns>
        ///     Number of bytes written.
        /// </returns>
        public int WriteBytes(IntPtr address, byte[] bytes)
        {
            if (NativeMethods.VirtualProtectEx(ProcessHandle, address, (IntPtr)bytes.Length, PageProtection.PAGE_READWRITE, out uint oldProtect))
            {
                bool success = NativeMethods.WriteProcessMemory(ProcessHandle, address, bytes, bytes.Length, out int numWritten);
                NativeMethods.VirtualProtectEx(ProcessHandle, address, (IntPtr)bytes.Length, oldProtect, out oldProtect);
                if (!success || numWritten != bytes.Length)
                {
                    throw new AccessViolationException(string.Format("Could not write! {0} to {1} [{2}]", bytes.Length, address.ToString("X8"), new Win32Exception(Marshal.GetLastWin32Error()).Message));
                }
                return numWritten;
            }
            throw new AccessViolationException(string.Format("Could not write! VirtualProtectEx is failed! {0} to {1} [{2}]", bytes.Length, address.ToString("X8"), new Win32Exception(Marshal.GetLastWin32Error()).Message));
        }

        /// <summary>
        ///     Allocates memory inside the process.
        /// </summary>
        /// <param name="size">
        ///     Number of bytes to allocate
        /// </param>
        /// <param name="allocationType">
        ///     Type of memory allocation
        /// </param>
        /// <param name="protectionType">
        ///     Type of memory protection
        /// </param>
        /// <returns>Returns NULL on failure, or the base address of the allocated memory on success.</returns>
        public IntPtr AllocateMemory(
            int size,
            Interfaces.Data.MemoryAllocationType allocationType = Interfaces.Data.MemoryAllocationType.MEM_COMMIT,
            Interfaces.Data.MemoryProtectionType protectionType = Interfaces.Data.MemoryProtectionType.PAGE_EXECUTE_READWRITE)
        {
            return NativeMethods.VirtualAllocEx(ProcessHandle, IntPtr.Zero, size, (MemoryAllocationType)allocationType, (MemoryProtectionType)protectionType);
        }

        /// <summary>
        ///     Frees an allocated block of memory in the process.
        /// </summary>
        /// <param name="address">
        ///     Address of the block of memory
        /// </param>
        /// <returns>
        ///     Returns true on success, false overwise
        /// </returns>
        public bool FreeMemory(IntPtr address)
        {
            // 0 for MEM_RELEASE
            return FreeMemory(address, 0, Interfaces.Data.MemoryFreeType.MEM_RELEASE);
        }

        /// <summary>
        ///     Frees an allocated block of memory in the process.
        /// </summary>
        /// <param name="address">
        ///     Address of the block of memory
        /// </param>
        /// <param name="size">
        ///     Number of bytes to be freed. This must be 0 if using MEM_RELEASE
        /// </param>
        /// <param name="freeType">
        ///     Type of free operation
        /// </param>
        /// <returns>
        ///     Returns true on success, false overwise
        /// </returns>
        public bool FreeMemory(IntPtr address, int size, Interfaces.Data.MemoryFreeType freeType)
        {
            // for sure
            if (freeType == Interfaces.Data.MemoryFreeType.MEM_RELEASE)
                size = 0;
            return NativeMethods.VirtualFreeEx(ProcessHandle, address, size, (MemoryFreeType)freeType);
        }

        public void Dispose()
        {
            try
            {
                try
                {
                    Process.LeaveDebugMode();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.ToString());
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }

        /// <summary>
        ///     Checks if memory is really physically backed
        ///     It is needed if target process uses some kind of trap pages
        /// </summary>
        /// <param name="address">
        ///     Address of the block of memory to check
        /// </param>
        /// <param name="count">
        ///     Size of block of memory
        /// </param>
        private void ThrowIfMemoryIsNotPhysicallyBacked(IntPtr address, int count)
        {
            uint pageSize = (uint)Environment.SystemPageSize;
            int startPage = (int)Math.Floor((double)address.ToInt64() / pageSize);
            int numPages = (int)Math.Ceiling((float)count / pageSize);
            long startPtr = pageSize * startPage;
            _PSAPI_WORKING_SET_EX_INFORMATION[] wsInfo = new _PSAPI_WORKING_SET_EX_INFORMATION[numPages];
            for (uint i = 0; i < numPages; i++)
            {
                wsInfo[i] = new _PSAPI_WORKING_SET_EX_INFORMATION
                {
                    VirtualAddress = new IntPtr(startPtr + i * pageSize)
                };
            }
            if (!NativeMethods.QueryWorkingSetEx(ProcessHandle, wsInfo, numPages * sizeof(_PSAPI_WORKING_SET_EX_INFORMATION)))
                throw new UnableToReadMemoryException(address, "You cannot read this address because QueryWorkingSetEx returned with error");
            foreach (_PSAPI_WORKING_SET_EX_INFORMATION info in wsInfo)
                if (info.VirtualAttributes.Valid != 1)
                    throw new UnableToReadMemoryException(address, "You cannot read this address because related memory page is not backed by physical memory");
        }

    }
}
