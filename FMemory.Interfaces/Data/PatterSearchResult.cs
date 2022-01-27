using System;

namespace FMemory.Interfaces.Data
{
    /// <summary>
    ///     Contains result of successfully found pattern
    /// </summary>
    public struct PatterSearchResult
    {
        /// <summary>
        ///     Address of POI
        /// </summary>
        public IntPtr Address;

        /// <summary>
        ///     Address of instruction, where reference to POI was found
        /// </summary>
        public IntPtr UnmodifiedAddress;

    }
}
