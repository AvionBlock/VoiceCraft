using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VoiceCraft.Core
{
    public static class Extensions
    {
        public static string Truncate(this string value, int maxLength, string truncationSuffix = "…")
        {
            return value.Length > maxLength
                ? value[..maxLength] + truncationSuffix
                : value;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(this Vector<T> data, Span<T> destination) where T : struct
        {
            if (destination.Length < Vector<T>.Count)
            {
                throw new ArgumentException("Argument too short!", nameof(destination));
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), data);
        }
    }
}