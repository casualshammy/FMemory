#nullable enable
using FMemory.Common.Data;
using FMemory.Common.Interfaces;
using FMemory.PatternHelpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace FMemory;

/// <summary>
///     Main class for finding memory address by pattern
///     Usage: get instance of <see cref="MemoryPattern"/>, then call <see cref="Find"/>
/// </summary>
public class MemoryPattern : IMemoryPattern
{
  private readonly long p_cacheSize;
  private readonly byte[] p_bytes;
  private readonly bool[] p_mask;
  private readonly List<IPatternModifier> p_modifiers;

  /// <summary>
  ///     Creates instance of <see cref="MemoryPattern"/> from text string.
  ///     <para></para>
  ///     String must be formatted per byte as "E8 xx xx xx xx A5 ?? ?? ?? ??", where:
  ///       - "E8", "A5" - known value (it does not have to be E8 or A5! It's just an example!)
  ///       - "xx" - unknown value, and we don't care about it
  ///       - "??" - unknown value, and we look exactly for it (we will look only for first occurence of sequence of "??")
  /// </summary>
  /// <param name="_name">Name of the pattern</param>
  /// <param name="_textPattern">String with pattern definition</param>
  /// <param name="_modifiers">Set of <see cref="IPatternModifier"/> address modificators</param>
  public MemoryPattern(string _name, string _textPattern, params IPatternModifier[]? _modifiers)
  {
    Name = _name;
    p_modifiers = _modifiers?.ToList() ?? new List<IPatternModifier>();

    var maskEntries = _textPattern.Split(' ');
    
    p_bytes = new byte[maskEntries.Length];
    p_mask = new bool[maskEntries.Length];
    p_cacheSize = Math.Max(4*1024, maskEntries.Length);

    var index = 0;
    var addModifierIndex = 0u;
    var addModifierInitialised = false;
    foreach (string token in maskEntries)
    {
      if (token.Length > 2)
        throw new InvalidDataException("Invalid token: " + token);

      if (token.Contains('x'))
      {
        p_mask[index++] = false;
      }
      else if (token.Contains('?'))
      {
        p_mask[index++] = false;
        if (!addModifierInitialised)
        {
          // index matters!
          p_modifiers.Insert(0, new AddModifier(addModifierIndex));
          addModifierInitialised = true;
        }
      }
      else
      {
        byte byteData = byte.Parse(token, NumberStyles.HexNumber);
        p_bytes[index] = byteData;
        p_mask[index] = true;
        index++;
      }
      addModifierIndex++;
    }
  }

  /// <summary>
  ///     Name of pattern
  /// </summary>
  public string Name { get; }

  /// <summary>
  ///     List of address modifiers for pattern
  /// </summary>
  public IReadOnlyList<IPatternModifier> Modifiers => p_modifiers;

  /// <summary>
  ///     Returns relative memory addresses for pattern
  /// </summary>
  /// <param name="_mm">Instance of <see cref="MemoryManager"/> to use for search</param>
  /// <returns>Enumerable of <see cref="Result"/> with found addresses</returns>
  public IEnumerable<PatterSearchResult> Find(IMemoryManager _mm)
  {
    var mainModule = _mm.Process.MainModule;
    if (mainModule == null)
      throw new InvalidOperationException($"Can't get main module of process '{_mm.Process.Id}'!");

    var baseAddress = (long)mainModule.BaseAddress;
    var foundAddresses = new HashSet<IntPtr>();
    foreach (var address in FindMaskAddress(_mm))
    {
      var startAddress = address;
      foreach (var modifier in p_modifiers)
        startAddress = modifier.Apply(_mm, startAddress);

      var relativeAddress = new IntPtr((long)startAddress - baseAddress);
      if (!foundAddresses.Contains(relativeAddress))
      {
        foundAddresses.Add(relativeAddress);
        yield return new PatterSearchResult(relativeAddress, new IntPtr(address.ToInt64() - baseAddress));
      }
    }
  }

  private bool DataCompare(byte[] _data, uint _dataOffset)
  {
    return !p_mask
      .Where((_t, _i) => _t && p_bytes[_i] != _data[_dataOffset + _i])
      .Any();
  }

  private IEnumerable<IntPtr> FindMaskAddress(IMemoryManager _mm)
  {
    var mainModule = _mm.Process.MainModule;
    if (mainModule == null)
      throw new InvalidOperationException($"Can't get main module of process '{_mm.Process.Id}'!");

    var mainModuleBaseAddress = mainModule.BaseAddress;
    var mainModuleSize = mainModule.ModuleMemorySize;
    var patternLength = p_bytes.LongLength;

    for (var offset = 0L; offset < mainModuleSize - patternLength; offset += p_cacheSize - patternLength)
    {
      var cacheSize = p_cacheSize > mainModuleSize - offset ? mainModuleSize - offset : p_cacheSize;
      var cacheBytes = _mm.ReadBytes(mainModuleBaseAddress + (int)offset, (int)cacheSize);
      for (var offsetInCacheBytes = 0u; offsetInCacheBytes < cacheBytes.Length - patternLength; offsetInCacheBytes++)
        if (DataCompare(cacheBytes, offsetInCacheBytes))
          yield return mainModuleBaseAddress + (int)(offset + offsetInCacheBytes);
    }
  }

}
