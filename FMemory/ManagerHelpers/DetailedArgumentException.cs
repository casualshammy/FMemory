using System;

namespace FMemory.Helpers
{
    /// <summary>
    ///     ArgumentException with additional text field
    /// </summary>
    public class DetailedArgumentException : ArgumentException
    {
        public string Notice;

        internal DetailedArgumentException(string message, string paramName, string notice) : base(message, paramName)
        {
            Notice = notice;
        }
    }
}
