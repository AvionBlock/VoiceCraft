using System;
using System.Linq;
using System.Numerics;
using System.Text;

namespace VoiceCraft.Network;

public static class McApiStringCodec
{
    // Printable ASCII only, excluding characters known to be problematic for MCBE/data tunnel transport.
    private const string Alphabet =
        "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz!#$&'()*+,-./:;<=>?@[]^_`{}~";

    private static readonly BigInteger Base = Alphabet.Length;
    private static readonly int[] ReverseLookup = BuildReverseLookup();
    internal static int AlphabetSize => Alphabet.Length;

    public static string Encode(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
            return string.Empty;

        var leadingZeroCount = 0;
        while (leadingZeroCount < data.Length && data[leadingZeroCount] == 0)
            leadingZeroCount++;

        var builder = new StringBuilder(data.Length * 2);
        for (var i = 0; i < leadingZeroCount; i++)
            builder.Append(Alphabet[0]);

        if (leadingZeroCount == data.Length)
            return builder.ToString();

        var magnitude = new byte[data.Length - leadingZeroCount + 1];
        for (var i = 0; i < data.Length - leadingZeroCount; i++)
            magnitude[i] = data[data.Length - 1 - i];

        var value = new BigInteger(magnitude);
        Span<char> buffer = stackalloc char[512];

        while (value > BigInteger.Zero)
        {
            value = BigInteger.DivRem(value, Base, out var remainder);
            if (buffer.Length == 0)
                throw new InvalidOperationException("Unexpected buffer exhaustion.");
            builder.Append(Alphabet[(int)remainder]);
        }

        if (builder.Length == leadingZeroCount)
            builder.Append(Alphabet[0]);

        var chars = builder.ToString().ToCharArray();
        Array.Reverse(chars, leadingZeroCount, chars.Length - leadingZeroCount);
        return new string(chars);
    }

    public static byte[] Decode(string data)
    {
        if (string.IsNullOrEmpty(data))
            return [];

        var leadingZeroCount = 0;
        while (leadingZeroCount < data.Length && data[leadingZeroCount] == Alphabet[0])
            leadingZeroCount++;

        var value = BigInteger.Zero;
        for (var i = leadingZeroCount; i < data.Length; i++)
        {
            var current = data[i];
            if (current >= ReverseLookup.Length || ReverseLookup[current] < 0)
                throw new ArgumentException($"Character '{current}' is not valid in an encoded McApi payload.",
                    nameof(data));

            value *= Base;
            value += ReverseLookup[current];
        }

        var decoded = value == BigInteger.Zero ? [] : value.ToByteArray(isUnsigned: true, isBigEndian: true);
        if (leadingZeroCount == 0)
            return decoded;

        var output = new byte[leadingZeroCount + decoded.Length];
        if (decoded.Length > 0)
            Buffer.BlockCopy(decoded, 0, output, leadingZeroCount, decoded.Length);
        return output;
    }

    public static bool IsSafePayloadCharacter(char value)
    {
        return value < ReverseLookup.Length && ReverseLookup[value] >= 0;
    }

    internal static char GetAlphabetChar(int index)
    {
        if (index < 0 || index >= Alphabet.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        return Alphabet[index];
    }

    internal static int GetAlphabetIndex(char value)
    {
        if (value >= ReverseLookup.Length || ReverseLookup[value] < 0)
            throw new ArgumentException($"Character '{value}' is not valid in an encoded McApi payload.",
                nameof(value));
        return ReverseLookup[value];
    }

    private static int[] BuildReverseLookup()
    {
        var lookup = Enumerable.Repeat(-1, 128).ToArray();
        for (var i = 0; i < Alphabet.Length; i++)
            lookup[Alphabet[i]] = i;
        return lookup;
    }
}
