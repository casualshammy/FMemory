using FMemory.Interfaces.Data;
using System;
using System.Diagnostics;

namespace FMemory.Interfaces
{
    public interface IMemoryManager
    {
        bool AvoidNotPhysicallyBackedTrapPages { get; set; }
        Process Process { get; }
        IntPtr ImageBase { get; }

        IntPtr AllocateMemory(int size, MemoryAllocationType allocationType = MemoryAllocationType.MEM_COMMIT, MemoryProtectionType protectionType = MemoryProtectionType.PAGE_EXECUTE_READWRITE);
        void Dispose();
        bool FreeMemory(IntPtr address);
        bool FreeMemory(IntPtr address, int size, MemoryFreeType freeType);
        T Read<T>(IntPtr address) where T : struct;
        byte[] ReadBytes(IntPtr address, int count);
        bool Write<T>(IntPtr address, T value);
        int WriteBytes(IntPtr address, byte[] bytes);
    }

}