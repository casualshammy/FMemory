using FMemory.Interfaces.Data;
using System.Collections.Generic;

namespace FMemory.Interfaces
{
    public interface IMemoryPattern
    {
        string Name { get; }

        IEnumerable<PatterSearchResult> Find(IMemoryManager bm);
    }
}