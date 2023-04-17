#nullable enable
using Ax.Fw.Windows.WinAPI;
using FMemory.Common.Data;
using FMemory.Common.Interfaces;
using FMemory.Helpers;
using FMemory.Toolkit;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Text;

namespace FMemory;

/// <summary>
///     Main class for memory interactions
/// </summary>
public sealed unsafe class MemoryManager : IDisposable, IMemoryManager
{
  /// <summary>
  ///     Initializes a new instance of the <see cref="MemoryManager" /> class.
  /// </summary>
  /// <param name="_process">The process</param>
  public MemoryManager(Process _process)
  {
    if (_process.HasExited)
      throw new AccessViolationException($"Process: {_process.Id} has already exited. Can not attach to it.");

    if (_process.MainModule == null)
      throw new InvalidOperationException($"Process: {_process.Id} has not main module.");

    Process.EnterDebugMode();
    Process = _process;
    var openFlags = ProcessAccessFlags.PROCESS_CREATE_THREAD |
                    ProcessAccessFlags.PROCESS_QUERY_INFORMATION |
                    ProcessAccessFlags.PROCESS_SET_INFORMATION | 
                    ProcessAccessFlags.PROCESS_TERMINATE |
                    ProcessAccessFlags.PROCESS_VM_OPERATION | 
                    ProcessAccessFlags.PROCESS_VM_READ |
                    ProcessAccessFlags.PROCESS_VM_WRITE | 
                    ProcessAccessFlags.SYNCHRONIZE;
    ProcessHandle = NativeMethods.OpenProcess(openFlags, false, _process.Id);
    ImageBase = Process.MainModule.BaseAddress;
  }

  /// <summary>
  ///     Gets the process handle
  /// </summary>
  public IntPtr ProcessHandle { get; }

  /// <summary>
  ///     Gets the process.
  /// </summary>
  public Process Process { get; }

  /// <summary>
  ///     Gets the address of image base.
  /// </summary>
  public IntPtr ImageBase { get; }

  /// <summary>
  ///     If set to true, every <see cref="ReadBytes"/> and <see cref="Read"/> will check if physical pages are backing allocation of memory
  /// </summary>
  public bool AvoidNotPhysicallyBackedTrapPages { get; set; }

  /// <summary>
  ///     Reads a specific number of bytes from memory
  /// </summary>
  /// <param name="_address">The address in memory</param>
  /// <param name="_count">The count of bytes to read</param>
  public byte[] ReadBytes(IntPtr _address, int _count)
  {
    if (_count != 0)
    {
      // Yes, this address is valid, but... Well, if you really want it, just delete next instruction
      if (_address == IntPtr.Zero)
      {
        var stackTrace = new StackTrace(1, true);
        throw new DetailedArgumentException("Address cannot be zero.", nameof(_address), stackTrace.ToString());
      }
      if (AvoidNotPhysicallyBackedTrapPages)
        ThrowIfMemoryIsNotPhysicallyBacked(_address, _count);

      var buffer = new byte[_count]; // ArrayPool<byte> is NOT effective here (tested)
      fixed (byte* buf = buffer)
        if (NativeMethods.ReadProcessMemory(ProcessHandle, _address, buf, _count, out int numRead) && numRead == _count)
          return buffer;

      var lastError = Marshal.GetLastWin32Error();
      throw new Win32Exception(lastError, $"Could not read bytes from 0x{_address.ToString("X")}");
    }
    return Array.Empty<byte>();
  }

  /// <summary>
  ///     Reads a value from the address in memory
  /// </summary>
  /// <param name="_address">
  ///     The address to read
  /// </param>
  public T Read<T>(IntPtr _address) where T : struct
  {
    fixed (byte* buffer = ReadBytes(_address, MarshalCache<T>.Size))
      return InternalRead<T>((IntPtr)buffer);
  }

  /// <summary>
  /// Read null-terminated string
  /// </summary>
  /// <param name="_address">Address of the start of string</param>
  /// <param name="_maximumSize">Maximum size of string (in bytes, not characters!)</param>
  /// <param name="_encoding">Encoding of string. Default is UTF8</param>
  /// <exception cref="InvalidOperationException">Throws if value of <param name="_maximumSize"> is unsufficient</exception>
  public string ReadString(IntPtr _address, int _maximumSize, Encoding? _encoding = default)
  {
    var bytes = ReadBytes(_address, _maximumSize);
    var nullIndex = bytes.FirstIndexMatch(_ => _ == 0);
    if (nullIndex == -1)
      throw new InvalidOperationException($"Size of string was set to '{_maximumSize}', but string with address 0x{_address.ToInt64():X} is longer!");

    var result = (_encoding ?? Encoding.UTF8).GetString(bytes.AsSpan()[..nullIndex]);
    return result;
  }

  /// <summary>
  ///     Writes a value to the address in memory
  /// </summary>
  /// <param name="_address">
  ///     The address in memory
  /// </param>
  /// <param name="_value">
  ///     The value to write
  /// </param>
  /// <returns>
  ///     True if it succeeds, false overwise
  /// </returns>
  public void Write<T>(IntPtr _address, T _value) where T : notnull
  {
    byte[] buffer;
    var allocation = Marshal.AllocHGlobal(MarshalCache<T>.Size);
    try
    {
      Marshal.StructureToPtr(_value, allocation, false);
      buffer = new byte[MarshalCache<T>.Size];
      Marshal.Copy(allocation, buffer, 0, MarshalCache<T>.Size);
    }
    finally
    {
      Marshal.FreeHGlobal(allocation);
    }

    var size = MarshalCache<T>.Size;

    // Fix the protection flags to EXECUTE_READWRITE!
    if (!NativeMethods.VirtualProtectEx(ProcessHandle, _address, (IntPtr)size, PageProtection.PAGE_READWRITE, out uint oldProtect))
      throw new AccessViolationException(string.Format(
        "Could not write! VirtualProtectEx is failed! {0} bytes to {1} [{2}]",
        buffer.Length, 
        _address.ToString("X8"), 
        new Win32Exception(Marshal.GetLastWin32Error()).Message));

    var success = NativeMethods.WriteProcessMemory(ProcessHandle, _address, buffer, size, out int numWritten);
    NativeMethods.VirtualProtectEx(ProcessHandle, _address, (IntPtr)size, oldProtect, out _);

    if (!success || numWritten != size)
      throw new AccessViolationException($"Could not write! Value of '{typeof(T).Name}' to '0x{_address:X8}' ({new Win32Exception(Marshal.GetLastWin32Error()).Message})");
  }

  /// <summary>
  ///     Writes an array of bytes to memory.
  /// </summary>
  /// <param name="_address">
  ///     The address to write to</param>
  /// <param name="_bytes">
  ///     The byte array to write
  /// </param>
  /// <returns>
  ///     Number of bytes written.
  /// </returns>
  public void WriteBytes(IntPtr _address, byte[] _bytes)
  {
    if (!NativeMethods.VirtualProtectEx(ProcessHandle, _address, (IntPtr)_bytes.Length, PageProtection.PAGE_READWRITE, out uint oldProtect))
      throw new AccessViolationException(string.Format(
        "Could not write! VirtualProtectEx is failed! {0} bytes to {1} [{2}]",
        _bytes.Length,
        _address.ToString("X8"),
        new Win32Exception(Marshal.GetLastWin32Error()).Message));

    var success = NativeMethods.WriteProcessMemory(ProcessHandle, _address, _bytes, _bytes.Length, out int numWritten);
    NativeMethods.VirtualProtectEx(ProcessHandle, _address, (IntPtr)_bytes.Length, oldProtect, out _);
    if (!success || numWritten != _bytes.Length)
      throw new AccessViolationException($"Could not write! '{_bytes.Length}' bytes to '0x{_address:X8}' ({new Win32Exception(Marshal.GetLastWin32Error()).Message})");
  }

  /// <summary>
  ///     Allocates memory inside the process.
  /// </summary>
  /// <param name="_size">
  ///     Number of bytes to allocate
  /// </param>
  /// <param name="_allocationType">
  ///     Type of memory allocation
  /// </param>
  /// <param name="_protectionType">
  ///     Type of memory protection
  /// </param>
  /// <returns>Returns NULL on failure, or the base address of the allocated memory on success.</returns>
  public IntPtr AllocateMemory(
      int _size,
      Common.Data.MemoryAllocationType _allocationType = Common.Data.MemoryAllocationType.MEM_COMMIT,
      Common.Data.MemoryProtectionType _protectionType = Common.Data.MemoryProtectionType.PAGE_EXECUTE_READWRITE)
  {
    return NativeMethods.VirtualAllocEx(
      ProcessHandle, 
      IntPtr.Zero, 
      _size, 
      (Ax.Fw.Windows.WinAPI.MemoryAllocationType)_allocationType, 
      (Ax.Fw.Windows.WinAPI.MemoryProtectionType)_protectionType);
  }

  public IDisposable AllocateMemory(
      int _size,
      out IntPtr _allocatedMemoryAddress,
      Common.Data.MemoryAllocationType _allocationType = Common.Data.MemoryAllocationType.MEM_COMMIT,
      Common.Data.MemoryProtectionType _protectionType = Common.Data.MemoryProtectionType.PAGE_EXECUTE_READWRITE)
  {
    var allocation = AllocateMemory(_size, _allocationType, _protectionType);

    _allocatedMemoryAddress = allocation;

    return Disposable.Create(() => FreeMemory(allocation));
  }

  /// <summary>
  ///     Frees an allocated block of memory in the process.
  /// </summary>
  /// <param name="_address">
  ///     Address of the block of memory
  /// </param>
  /// <returns>
  ///     Returns true on success, false overwise
  /// </returns>
  public bool FreeMemory(IntPtr _address)
  {
    // 0 for MEM_RELEASE
    return FreeMemory(_address, 0, Common.Data.MemoryFreeType.MEM_RELEASE);
  }

  /// <summary>
  ///     Frees an allocated block of memory in the process.
  /// </summary>
  /// <param name="_address">
  ///     Address of the block of memory
  /// </param>
  /// <param name="_size">
  ///     Number of bytes to be freed. This must be 0 if using MEM_RELEASE
  /// </param>
  /// <param name="_freeType">
  ///     Type of free operation
  /// </param>
  /// <returns>
  ///     Returns true on success, false overwise
  /// </returns>
  public bool FreeMemory(IntPtr _address, int _size, Common.Data.MemoryFreeType _freeType)
  {
    if (_freeType == Common.Data.MemoryFreeType.MEM_RELEASE)
      _size = 0;

    return NativeMethods.VirtualFreeEx(
      ProcessHandle,      _address, 
      _size, 
      (Ax.Fw.Windows.WinAPI.MemoryFreeType)_freeType);
  }

  public void Dispose()
  {
    try
    {
      NativeMethods.CloseHandle(ProcessHandle);
    }
    catch (Exception ex)
    {
      Trace.WriteLine(ex.ToString());
#if DEBUG
      throw;
#endif
    }
  }

  private T InternalRead<T>(IntPtr _address) where T : struct
  {
    try
    {
      if (_address == IntPtr.Zero)
        throw new InvalidOperationException("Cannot retrieve a value at address 0");

      object returnValue;
      switch (MarshalCache<T>.TypeCode)
      {
        case TypeCode.Object:

          if (MarshalCache<T>.RealType == typeof(IntPtr))
            return (T)(object)*(IntPtr*)_address;

          if (!MarshalCache<T>.TypeRequiresMarshal)
          {
            T obj = default;
            void* ptr = MarshalCache<T>.GetUnsafePtr(ref obj);
            NativeMethods.MoveMemory(ptr, (void*)_address, MarshalCache<T>.Size);
            return obj;
          }
          // All System.Object's require marshaling!
          var value = Marshal.PtrToStructure(_address, typeof(T));
          if (value == null)
            throw new InvalidCastException($"Can't read type '{typeof(T).Name}' from the address '0x{_address:X8}'");

          returnValue = value;
          break;
        case TypeCode.SByte:
          returnValue = *(sbyte*)_address;
          break;
        case TypeCode.Byte:
          returnValue = *(byte*)_address;
          break;
        case TypeCode.Int16:
          returnValue = *(short*)_address;
          break;
        case TypeCode.UInt16:
          returnValue = *(ushort*)_address;
          break;
        case TypeCode.Int32:
          returnValue = *(int*)_address;
          break;
        case TypeCode.UInt32:
          returnValue = *(uint*)_address;
          break;
        case TypeCode.Int64:
          returnValue = *(long*)_address;
          break;
        case TypeCode.UInt64:
          returnValue = *(ulong*)_address;
          break;
        case TypeCode.Single:
          returnValue = *(float*)_address;
          break;
        case TypeCode.Double:
          returnValue = *(double*)_address;
          break;
        case TypeCode.Decimal:
          returnValue = *(decimal*)_address;
          break;
        case TypeCode.Boolean:
          returnValue = *(byte*)_address != 0;
          break;
        case TypeCode.Char:
          returnValue = *(char*)_address;
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
      return (T)returnValue;
    }
    catch (AccessViolationException)
    {
      return default;
    }
  }

  /// <summary>
  ///     Checks if memory is really physically backed
  ///     It is needed if target process uses some kind of trap pages
  /// </summary>
  /// <param name="_address">
  ///     Address of the block of memory to check
  /// </param>
  /// <param name="_count">
  ///     Size of block of memory
  /// </param>
  private void ThrowIfMemoryIsNotPhysicallyBacked(IntPtr _address, int _count)
  {
    uint pageSize = (uint)Environment.SystemPageSize;
    int startPage = (int)Math.Floor((double)_address.ToInt64() / pageSize);
    int numPages = (int)Math.Ceiling((float)_count / pageSize);
    long startPtr = pageSize * startPage;
    var wsInfo = new _PSAPI_WORKING_SET_EX_INFORMATION[numPages];
    for (uint i = 0; i < numPages; i++)
    {
      wsInfo[i] = new _PSAPI_WORKING_SET_EX_INFORMATION
      {
        VirtualAddress = new IntPtr(startPtr + i * pageSize)
      };
    }
    if (!NativeMethods.QueryWorkingSetEx(ProcessHandle, wsInfo, numPages * sizeof(_PSAPI_WORKING_SET_EX_INFORMATION)))
      throw new UnableToReadMemoryException(_address, "You cannot read this address because QueryWorkingSetEx returned with error");

    foreach (var info in wsInfo)
      if (info.VirtualAttributes.Valid != 1)
        throw new UnableToReadMemoryException(_address, "You cannot read this address because related memory page is not backed by physical memory");
  }

}
