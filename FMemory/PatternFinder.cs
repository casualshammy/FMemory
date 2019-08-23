using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace FMemory
{
    public class PatternFinder
    {
        public string Name { get; private set; }
        public byte[] Bytes { get; private set; }
        public bool[] Mask { get; private set; }
        private const long CacheSize = 0x500;
        public List<IModifier> Modifiers = new List<IModifier>();

        public struct Result
        {
            public IntPtr address;
            public IntPtr unmodifiedAddress;
        }

        private bool DataCompare(byte[] data, uint dataOffset)
        {
            return !Mask.Where((t, i) => t && Bytes[i] != data[dataOffset + i]).Any();
        }

        private IEnumerable<IntPtr> FindStart(MemoryManagement.MemoryManager bm)
        {
            ProcessModule mainModule = bm.Process.MainModule;
            IntPtr start = mainModule.BaseAddress;
            long size = mainModule.ModuleMemorySize;
            long patternLength = Bytes.LongLength;

            List<IntPtr> addresses = new List<IntPtr>();

            for (long i = 0; i < size - patternLength; i += CacheSize - patternLength)
            {
                byte[] cache = bm.ReadBytes(start + (int)i, (int)(CacheSize > size - i ? size - i : CacheSize));
                for (uint i2 = 0; i2 < cache.Length - patternLength; i2++)
                {
                    if (DataCompare(cache, i2))
                    {
                        addresses.Add(start + (int)(i + i2));
                    }
                }
            }
            if (addresses.Count > 0)
            {
                return addresses.AsEnumerable();
            }
            throw new InvalidDataException($"Pattern {Name} not found");
        }

        public IEnumerable<Result> Find(MemoryManagement.MemoryManager bm)
        {
            HashSet<IntPtr> foundAddresses = new HashSet<IntPtr>();
            foreach (IntPtr intPtr in FindStart(bm))
            {
                IntPtr start = intPtr;
                foreach (IModifier modifier in Modifiers)
                {
                    start = modifier.Apply(bm, start);
                }
                IntPtr address = new IntPtr((long)start - (long)bm.Process.MainModule.BaseAddress);
                if (!foundAddresses.Contains(address))
                {
                    foundAddresses.Add(address);
                    yield return new Result { address = address, unmodifiedAddress = new IntPtr(intPtr.ToInt64() - bm.Process.MainModule.BaseAddress.ToInt64()) };
                }
            }
        }

        public static Pattern FromTextstyle(string name, string pattern, params IModifier[] modifiers)
        {
            Pattern ret = new Pattern { Name = name };
            if (modifiers != null)
                ret.Modifiers = modifiers.ToList();
            string[] split = pattern.Split(' ');
            int index = 0;
            ret.Bytes = new byte[split.Length];
            ret.Mask = new bool[split.Length];
            uint addModifierIndex = 0;
            bool addModifierInitialised = false;
            foreach (string token in split)
            {
                if (token.Length > 2)
                    throw new InvalidDataException("Invalid token: " + token);
                if (token.Contains("x"))
                {
                    ret.Mask[index++] = false;
                }
                else if (token.Contains("?"))
                {
                    ret.Mask[index++] = false;
                    if (!addModifierInitialised)
                    {
                        ret.Modifiers.Insert(0, new AddModifier(addModifierIndex)); // index matters
                        addModifierInitialised = true;
                    }
                }
                else
                {
                    byte data = byte.Parse(token, NumberStyles.HexNumber);
                    ret.Bytes[index] = data;
                    ret.Mask[index] = true;
                    index++;
                }
                addModifierIndex++;
            }
            return ret;
        }
    }

    public interface IModifier
    {
        IntPtr Apply(MemoryManagement.MemoryManager bm, IntPtr address);
    }

    public class AddModifier : IModifier
    {
        public uint Offset { get; private set; }

        public AddModifier(uint val)
        {
            Offset = val;
        }

        public IntPtr Apply(MemoryManagement.MemoryManager bm, IntPtr addr)
        {
            return addr + (int)Offset;
        }
    }

    public enum LeaType
    {
        Byte,
        Word,
        Dword,
        E8,
        SimpleAddress,
        Cmp,
        CmpMinusOne,
        RelativePlus8,
    }

    public class LeaModifier : IModifier
    {
        public LeaType Type { get; private set; }

        public LeaModifier(LeaType type)
        {
            Type = type;
        }

        public IntPtr Apply(MemoryManagement.MemoryManager bm, IntPtr address)
        {
            switch (Type)
            {
                case LeaType.Byte:
                    return (IntPtr)bm.Read<byte>(address);

                case LeaType.Word:
                    return (IntPtr)bm.Read<ushort>(address);

                case LeaType.Dword:
                    return (IntPtr)bm.Read<uint>(address);

                case LeaType.E8:
                    return address + 4 + bm.Read<int>(address); // 4 = <call instruction size> - <E8>
                case LeaType.SimpleAddress:
                    return address;

                case LeaType.Cmp:
                    return address + 5 + bm.Read<int>(address);

                case LeaType.CmpMinusOne:
                    return address + 4 + bm.Read<int>(address);

                case LeaType.RelativePlus8:
                    return address + 8 + bm.Read<int>(address);
            }
            throw new InvalidDataException("Unknown LeaType");
        }
    }
}