namespace FMemory.PatternHelpers
{
    /// <summary>
    ///     Type of instruction
    ///     For example, if you want to find an address of function by (E8 + 4 bytes) instruction,
    ///     then you should use LeaType.E8
    /// </summary>
    public enum LeaType
    {
        Byte,
        Word,
        Dword,
        E8,

        /// <summary>
        ///     Just return raw address of instruction
        /// </summary>
        SimpleAddress,
        
        Cmp,
        RelativePlus8,
    }
}
