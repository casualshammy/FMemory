using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FMemory.Helpers
{
  /// <summary>
  ///     We use this class for speed-up marshaling
  /// </summary>
  public static class MarshalCache<T>
  {
    internal unsafe delegate void* GetUnsafePtrDelegate(ref T value);
    internal static readonly GetUnsafePtrDelegate GetUnsafePtr;

    static MarshalCache()
    {
      TypeCode = Type.GetTypeCode(typeof(T));
      if (typeof(T) == typeof(bool))
      {
        Size = 1;
        RealType = typeof(T);
      }
      else if (typeof(T).IsEnum)
      {
        Type underlying = typeof(T).GetEnumUnderlyingType();
        Size = GetSizeOf(underlying);
        RealType = underlying;
        TypeCode = Type.GetTypeCode(underlying);
      }
      else
      {
        Size = GetSizeOf(typeof(T));
        RealType = typeof(T);
      }

      // Here, we will do manual work to determine if type REALLY requires marshaling
      TypeRequiresMarshal = RequiresMarshal(RealType);

      // Generate a method to get the address of a generic type. We'll use this when we call RtlMoveMemory later for much faster structure reads
      var method = new DynamicMethod(
          string.Format("GetPinnedPtr<{0}>", typeof(T).FullName.Replace(".", "<>")), typeof(void*), new[] { typeof(T).MakeByRefType() },
          typeof(MarshalCache<>).Module);
      ILGenerator generator = method.GetILGenerator();
      generator.Emit(OpCodes.Ldarg_0);
      generator.Emit(OpCodes.Conv_U);
      generator.Emit(OpCodes.Ret);
      GetUnsafePtr = (GetUnsafePtrDelegate)method.CreateDelegate(typeof(GetUnsafePtrDelegate));
    }

    /// <summary> 
    ///     The size of the type
    /// </summary>
    public static int Size { get; }

    /// <summary>
    ///     The underlying type
    /// </summary>
    public static Type RealType { get; }

    /// <summary> 
    ///     The code of the type
    /// </summary>
    public static TypeCode TypeCode { get; }

    /// <summary>
    ///     True if this type requires to map variables
    /// </summary>
    public static bool TypeRequiresMarshal { get; }

    private static int GetSizeOf(Type _type)
    {
      // In case if structure contains generic types INSIDE it. In such cases, Marshal.SizeOf will throw an exception
      try
      {
        return Marshal.SizeOf(_type);
      }
      catch
      {
        // We're using generic sub-types. Meh, okay...
        int totalSize = 0;
        foreach (FieldInfo field in _type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
          // Maybe it is a fixed-sized buffer?
          object[] attr = field.GetCustomAttributes(typeof(FixedBufferAttribute), false);
          if (attr.Length > 0)
            if (attr[0] is FixedBufferAttribute fba)
              totalSize += GetSizeOf(fba.ElementType) * fba.Length;
          totalSize += GetSizeOf(field.FieldType);
        }
        return totalSize;
      }
    }

    private static bool RequiresMarshal(Type _type)
    {
      foreach (FieldInfo fieldInfo in _type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
      {
        bool requires = fieldInfo.GetCustomAttributes(typeof(MarshalAsAttribute), true).Any();
        if (requires)
        {
          Debug.WriteLine(fieldInfo.FieldType.Name + " requires marshaling.");
          return true;
        }
        if (_type == typeof(IntPtr) || _type == typeof(string))
          continue;

        // Custom object? Okay, let's check it for marshaling requirements
        if (Type.GetTypeCode(_type) == TypeCode.Object)
          requires |= RequiresMarshal(fieldInfo.FieldType);

        if (requires)
        {
          Debug.WriteLine(fieldInfo.FieldType.Name + " requires marshaling.");
          return true;
        }
      }
      return false;
    }

  }
}
