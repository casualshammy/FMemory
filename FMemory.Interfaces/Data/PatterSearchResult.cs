using System;

namespace FMemory.Common.Data;

/// <summary>
///     Contains result of successfully found pattern
/// </summary>
public readonly struct PatterSearchResult
{
  public PatterSearchResult(IntPtr _address, IntPtr _unmodifiedAddress)
  {
    Address = _address;
    UnmodifiedAddress = _unmodifiedAddress;
  }

  /// <summary>
  ///     Address of POI
  /// </summary>
  public readonly IntPtr Address;

  /// <summary>
  ///     Address of instruction, where reference to POI was found
  /// </summary>
  public readonly IntPtr UnmodifiedAddress;

}
