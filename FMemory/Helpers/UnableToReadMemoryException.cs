using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMemory.Helpers
{
    public class UnableToReadMemoryException : Exception
    {
        public UnableToReadMemoryException(IntPtr address, string msg) : base(msg)
        {
            Address = address;
        }

        public IntPtr Address { get; private set; }

    }
}
