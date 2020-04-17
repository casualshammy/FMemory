namespace FMemory.PatternHelpers
{
    /// <summary>
    ///     Type of instruction
    ///     For example, if you want to find an address of function by (E8 + 4 bytes) instruction,
    ///     then you should use LeaType.E8
    /// </summary>
    public enum LeaType
    {
        /// <summary>
        ///     Resulting address = (IntPtr)Memory.Read<byte>(instruction_address);
        /// </summary>
        Byte,

        /// <summary>
        ///     Resulting address = (IntPtr)Memory.Read<ushort>(instruction_address);
        /// </summary>
        Word,

        /// <summary>
        ///    Resulting address = (IntPtr)Memory.Read<uint>(instruction_address);
        /// </summary>
        Dword,

        /// <summary>
        ///     Resulting address = instruction_address + 4 + Memory.Read<int>(instruction_address); 
        /// </summary>
        E8,

        /// <summary>
        ///     Just return raw address of instruction
        ///     Resulting address = instruction_address
        /// </summary>
        SimpleAddress,

        /// <summary>
        ///     Resulting address = instruction_address + 5 + Memory.Read<int>(instruction_address);
        /// </summary>
        Cmp,

        /// <summary>
        ///     Resulting address = instruction_address + 8 + Memory.Read<int>(instruction_address);
        /// </summary>
        RelativePlus8,
    }
}
