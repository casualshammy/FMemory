using FMemory;
using FMemory.PatternHelpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace FMemory
{
    /// <summary>
    ///     Main class for finding memory address by pattern
    ///     Usage: get instance of <see cref="MemoryPattern"/> by calling <see cref="FromTextstyle"/>, then call <see cref="Find"/>
    /// </summary>
    public class MemoryPattern
    {

        /// <summary>
        ///     Name of pattern
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///     List of address modifiers for pattern
        /// </summary>
        public List<IModifier> Modifiers = new List<IModifier>();

        private byte[] p_bytes;
        private bool[] p_mask;

        /// <summary>
        ///     How much bytes to read in each memory block
        /// </summary>
        private const long p_cacheSize = 0x500;

        private MemoryPattern()
        {

        }

        /// <summary>
        ///     Compares memory block with pattern
        /// </summary>
        /// <param name="data">Memory block</param>
        /// <param name="dataOffset">Offset in memory block</param>
        /// <returns>True if memory block satisfies pattern, false overwise</returns>
        private bool DataCompare(byte[] data, uint dataOffset)
        {
            return !p_mask.Where((t, i) => t && p_bytes[i] != data[dataOffset + i]).Any();
        }

        /// <summary>
        ///     Actually finds memory block for pattern
        /// </summary>
        /// <param name="bm">Instance of <see cref="MemoryManager"/> to use for search</param>
        /// <returns>Enumerable of <see cref="IntPtr"/> with found addresses</returns>
        private IEnumerable<IntPtr> FindMaskAddress(MemoryManager bm)
        {
            System.Diagnostics.ProcessModule mainModule = bm.Process.MainModule;
            IntPtr mainModuleBaseAddress = mainModule.BaseAddress;
            long mainModuleSize = mainModule.ModuleMemorySize;
            long patternLength = p_bytes.LongLength;

            for (long offset = 0; offset < mainModuleSize - patternLength; offset += p_cacheSize - patternLength)
            {
                byte[] cacheBytes = bm.ReadBytes(mainModuleBaseAddress + (int)offset, (int)(p_cacheSize > mainModuleSize - offset ? mainModuleSize - offset : p_cacheSize));
                for (uint offsetInCacheBytes = 0; offsetInCacheBytes < cacheBytes.Length - patternLength; offsetInCacheBytes++)
                {
                    if (DataCompare(cacheBytes, offsetInCacheBytes))
                    {
                        yield return mainModuleBaseAddress + (int)(offset + offsetInCacheBytes);
                    }
                }
            }
        }

        /// <summary>
        ///     Returns relative memory addresses for pattern
        /// </summary>
        /// <param name="bm">Instance of <see cref="MemoryManager"/> to use for search</param>
        /// <returns>Enumerable of <see cref="Result"/> with found addresses</returns>
        public IEnumerable<Result> Find(MemoryManager bm)
        {
            HashSet<IntPtr> foundAddresses = new HashSet<IntPtr>();
            foreach (IntPtr intPtr in FindMaskAddress(bm))
            {
                IntPtr startAddress = intPtr;
                foreach (IModifier modifier in Modifiers)
                {
                    startAddress = modifier.Apply(bm, startAddress);
                }
                IntPtr relativeAddress = new IntPtr((long)startAddress - (long)bm.Process.MainModule.BaseAddress);
                if (!foundAddresses.Contains(relativeAddress))
                {
                    foundAddresses.Add(relativeAddress);
                    yield return new Result { Address = relativeAddress, UnmodifiedAddress = new IntPtr(intPtr.ToInt64() - bm.Process.MainModule.BaseAddress.ToInt64()) };
                }
            }
        }

        /// <summary>
        ///     Creates instance of <see cref="MemoryPattern"/> from text string.
        ///     String must be formatted per byte as "E8 xx xx xx xx A5 ?? ?? ?? ??", where:
        ///       - "E8", "A5" - known value (it does not have to be E8 or A5! It's just an example!)
        ///       - "xx" - unknown value, and we don't care about it
        ///       - "??" - unknown value, and we look exactly for it (we will look only for first occurence of sequence of "??")
        /// </summary>
        /// <param name="name">Name of the pattern</param>
        /// <param name="textPattern">String with pattern definition</param>
        /// <param name="modifiers">Set of <see cref="IModifier"/> address modificators</param>
        /// <returns>Instance of <see cref="MemoryPattern"/></returns>
        public static MemoryPattern FromTextstyle(string name, string textPattern, params IModifier[] modifiers)
        {
            MemoryPattern pattern = new MemoryPattern { Name = name };
            if (modifiers != null)
                pattern.Modifiers = modifiers.ToList();
            string[] maskEntries = textPattern.Split(' ');
            int index = 0;
            pattern.p_bytes = new byte[maskEntries.Length];
            pattern.p_mask = new bool[maskEntries.Length];
            uint addModifierIndex = 0;
            bool addModifierInitialised = false;
            foreach (string token in maskEntries)
            {
                if (token.Length > 2)
                    throw new InvalidDataException("Invalid token: " + token);
                if (token.Contains("x"))
                {
                    pattern.p_mask[index++] = false;
                }
                else if (token.Contains("?"))
                {
                    pattern.p_mask[index++] = false;
                    if (!addModifierInitialised)
                    {
                        // index matters!
                        pattern.Modifiers.Insert(0, new AddModifier(addModifierIndex));
                        addModifierInitialised = true;
                    }
                }
                else
                {
                    byte byteData = byte.Parse(token, NumberStyles.HexNumber);
                    pattern.p_bytes[index] = byteData;
                    pattern.p_mask[index] = true;
                    index++;
                }
                addModifierIndex++;
            }
            return pattern;
        }

    }
}
