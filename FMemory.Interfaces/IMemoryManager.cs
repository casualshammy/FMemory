using FMemory.Interfaces.Data;
using System;
using System.Diagnostics;
using System.Text;

namespace FMemory.Interfaces
{
    public interface IMemoryManager
    {
        bool AvoidNotPhysicallyBackedTrapPages { get; set; }
        Process Process { get; }
        IntPtr ImageBase { get; }

        IntPtr AllocateMemory(int size, MemoryAllocationType allocationType = MemoryAllocationType.MEM_COMMIT, MemoryProtectionType protectionType = MemoryProtectionType.PAGE_EXECUTE_READWRITE);
        IDisposable AllocateMemory(int _size, out IntPtr _allocatedMemoryAddress, MemoryAllocationType _allocationType = MemoryAllocationType.MEM_COMMIT, MemoryProtectionType _protectionType = MemoryProtectionType.PAGE_EXECUTE_READWRITE);
        void Dispose();
        bool FreeMemory(IntPtr address);
        bool FreeMemory(IntPtr address, int size, MemoryFreeType freeType);
        T Read<T>(IntPtr address) where T : struct;
        byte[] ReadBytes(IntPtr address, int count);
        string ReadString(IntPtr _address, int _maximumSize, Encoding? _encoding = default);
        bool Write<T>(IntPtr address, T value) where T : notnull;
        int WriteBytes(IntPtr address, byte[] bytes);
    }

}