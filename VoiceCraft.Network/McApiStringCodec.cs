using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VoiceCraft.Network;

public static class McApiStringCodec
{
    private const char EscapeMarker = '\uE000';
    private const char PaddingMarker = '\uE001';

    private static readonly (int Start, int End)[] SafeRanges =
    [
        (0x0020, 0x0021),
        (0x0023, 0x005B),
        (0x005D, 0x007B),
        (0x007D, 0x007E),
        (0x0080, 0xD7FF),
        (0xE002, 0xFFFF)
    ];

    private static readonly int SafeCharCount = SafeRanges.Sum(range => range.End - range.Start + 1);

    public static string Encode(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
            return string.Empty;

        var builder = new StringBuilder(data.Length / 2 + 2);
        for (var i = 0; i < data.Length; i += 2)
        {
            var value = (ushort)(data[i] << 8);
            if (i + 1 < data.Length)
                value |= data[i + 1];

            AppendEncodedValue(builder, value);
        }

        if ((data.Length & 1) != 0)
            builder.Append(EscapeMarker).Append(PaddingMarker);

        return builder.ToString();
    }

    public static byte[] Decode(string data)
    {
        if (string.IsNullOrEmpty(data))
            return [];

        var bytes = new List<byte>(data.Length * 2);
        for (var i = 0; i < data.Length; i++)
        {
            var current = data[i];
            if (current != EscapeMarker)
            {
                AppendDecodedValue(bytes, GetSafeCharIndex(current));
                continue;
            }

            if (++i >= data.Length)
                throw new ArgumentException("Encoded string ended unexpectedly after an escape marker.", nameof(data));

            var escaped = data[i];
            if (escaped == PaddingMarker)
            {
                if (i != data.Length - 1)
                    throw new ArgumentException("Padding marker must appear only at the end of the encoded string.",
                        nameof(data));

                if (bytes.Count == 0)
                    throw new ArgumentException("Padding marker cannot be used without encoded payload.", nameof(data));

                bytes.RemoveAt(bytes.Count - 1);
                break;
            }

            AppendDecodedValue(bytes, SafeCharCount + GetSafeCharIndex(escaped));
        }

        return bytes.ToArray();
    }

    private static void AppendEncodedValue(StringBuilder builder, int value)
    {
        if (value < SafeCharCount)
        {
            builder.Append(GetSafeChar(value));
            return;
        }

        builder.Append(EscapeMarker);
        builder.Append(GetSafeChar(value - SafeCharCount));
    }

    private static void AppendDecodedValue(List<byte> bytes, int value)
    {
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }

    private static char GetSafeChar(int index)
    {
        if (index < 0 || index >= SafeCharCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        foreach (var (start, end) in SafeRanges)
        {
            var rangeLength = end - start + 1;
            if (index < rangeLength)
                return (char)(start + index);

            index -= rangeLength;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    private static int GetSafeCharIndex(char value)
    {
        var index = 0;
        foreach (var (start, end) in SafeRanges)
        {
            if (value >= start && value <= end)
                return index + value - start;

            index += end - start + 1;
        }

        throw new ArgumentException($"Character U+{(int)value:X4} is not valid in an encoded McApi payload.",
            nameof(value));
    }
}
