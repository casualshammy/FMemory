using FMemory.Common.Data;
using System;
using System.Diagnostics;
using System.Text;

namespace FMemory.Common.Interfaces;

public interface IMemoryManager
{
  bool AvoidNotPhysicallyBackedTrapPages { get; set; }
  Process Process { get; }
  IntPtr ImageBase { get; }

  IntPtr AllocateMemory(
    int _size,
    MemoryAllocationType _allocationType = MemoryAllocationType.MEM_COMMIT,
    MemoryProtectionType _protectionType = MemoryProtectionType.PAGE_EXECUTE_READWRITE);

  IDisposable AllocateMemory(
    int _size,
    out IntPtr _allocatedMemoryAddress,
    MemoryAllocationType _allocationType = MemoryAllocationType.MEM_COMMIT,
    MemoryProtectionType _protectionType = MemoryProtectionType.PAGE_EXECUTE_READWRITE);

  void Dispose();
  bool FreeMemory(IntPtr _address);
  bool FreeMemory(IntPtr _address, int size, MemoryFreeType _freeType);
  T Read<T>(IntPtr _address) where T : struct;
  byte[] ReadBytes(IntPtr _address, int _count);
  string ReadString(IntPtr _address, int _maximumSize, Encoding? _encoding = default);
  void Write<T>(IntPtr _address, T _value) where T : notnull;
  void WriteBytes(IntPtr _address, byte[] _bytes);
}