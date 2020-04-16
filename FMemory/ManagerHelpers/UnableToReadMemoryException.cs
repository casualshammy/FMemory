using System;

namespace FMemory.Helpers
{
    /// <summary>
    ///     Class for specific memory read exceptions (when we need to store address information)
    /// </summary>
    public class UnableToReadMemoryException : Exception
    {
        public UnableToReadMemoryException(IntPtr address, string msg) : base(msg)
        {
            Address = address;
        }

        public IntPtr Address { get; private set; }

    }
}
