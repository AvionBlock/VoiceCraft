using System;
using System.Collections.Generic;

namespace VoiceCraft.Network;

public static class Z85
{
    private const uint Div4 = Div1 * Div1 * Div1 * Div1;
    private const uint Div3 = Div1 * Div1 * Div1;
    private const uint Div2 = Div1 * Div1;
    private const uint Div1 = 85;
    private const int Byte3 = Byte1 * Byte1 * Byte1;
    private const int Byte2 = Byte1 * Byte1;
    private const int Byte1 = 256;

    /// <summary>
    ///     Encodes a byte array into a Z85 string with padding.
    /// </summary>
    /// <param name="data">The input byte array to encode</param>
    /// <returns> The Z85 encoded string</returns>
    /// <exception cref="ArgumentException"> Thrown when the input length is not a multiple of 4</exception>
    public static unsafe string GetStringWithPadding(ReadOnlySpan<byte> data)
    {
        var size = data.Length;
        var remainder = size % 4;

        if (remainder == 0)
            return GetString(data);

        var extraChars = remainder + 1;
        var encodedSize = (size - remainder) * 5 / 4 + extraChars;
        var destination = new string('0', encodedSize);
        var unpaddedSize = size - remainder;
        var charNum = 0;
        var byteNum = 0;

        fixed (char* z85Encoder = Z85Map.Encoder)
        fixed (char* z85Dest = destination)
        {
            uint value;
            while (byteNum < unpaddedSize)
            {
                value = (uint)(data[byteNum + 0] * Byte3 +
                               data[byteNum + 1] * Byte2 +
                               data[byteNum + 2] * Byte1 +
                               data[byteNum + 3]);
                byteNum += 4;

                z85Dest[charNum + 0] = z85Encoder[value / Div4 % Div1];
                z85Dest[charNum + 1] = z85Encoder[value / Div3 % Div1];
                z85Dest[charNum + 2] = z85Encoder[value / Div2 % Div1];
                z85Dest[charNum + 3] = z85Encoder[value / Div1 % Div1];
                z85Dest[charNum + 4] = z85Encoder[value % Div1];
                charNum += 5;
            }

            value = 0;
            while (byteNum < size)
                value = value * Byte1 + data[byteNum++];

            var divisor = (uint)Math.Pow(Div1, remainder);
            while (divisor != 0)
            {
                z85Dest[charNum++] = z85Encoder[value / divisor % Div1];
                divisor /= Div1;
            }
        }

        return destination;
    }

    /// <summary>
    ///     Encodes a byte array into a Z85 string. The input length must be a multiple of 4.
    /// </summary>
    /// <param name="data">The input byte array to encode</param>
    /// <returns> The Z85 encoded string</returns>
    /// <exception cref="ArgumentException"> Thrown when the input length is not a multiple of 4</exception>
    public static unsafe string GetString(ReadOnlySpan<byte> data)
    {
        var size = data.Length;
        var encodedSize = GetEncodedSize(size);
        var destination = new string('0', encodedSize);
        var charNum = 0;
        var byteNum = 0;

        fixed (char* z85Encoder = Z85Map.Encoder)
        fixed (char* z85Dest = destination)
        {
            while (byteNum < size)
            {
                var value = (uint)(data[byteNum + 0] * Byte3 +
                                   data[byteNum + 1] * Byte2 +
                                   data[byteNum + 2] * Byte1 +
                                   data[byteNum + 3]);
                byteNum += 4;

                z85Dest[charNum + 0] = z85Encoder[value / Div4 % Div1];
                z85Dest[charNum + 1] = z85Encoder[value / Div3 % Div1];
                z85Dest[charNum + 2] = z85Encoder[value / Div2 % Div1];
                z85Dest[charNum + 3] = z85Encoder[value / Div1 % Div1];
                z85Dest[charNum + 4] = z85Encoder[value % Div1];
                charNum += 5;
            }
        }

        return destination;
    }

    /// <summary>
    ///     Decodes a Z85 string into a byte array. The input length must be a multiple of 5 (+ 1 with padding).
    /// </summary>
    /// <param name="data"> The input Z85 string to decode</param>
    /// <returns> The decoded byte array</returns>
    /// <exception cref="ArgumentException"> Thrown when the input length is not a multiple of 5 (+ 1 with padding).</exception>
    public static unsafe byte[] GetBytesWithPadding(string data)
    {
        var size = (uint)data.Length;
        var remainder = size % 5;

        switch (remainder)
        {
            case 0:
                return GetBytes(data);
            case 1:
                throw new ArgumentException("Input length % 5 cannot be 1.");
        }
        
        var extraBytes = remainder - 1;
        var decodedSize = (int)((size - extraBytes) * 4 / 5 + extraBytes);
        var decoded = new byte[decodedSize];
        var charNum = 0;
        var byteNum = 0;
        uint value = 0;

        var size2 = size - remainder;

        // Get a pointers to avoid unnecessary range checking
        fixed (byte* z85Decoder = Z85Map.Decoder)
        fixed (char* input = data)
        {
            while (charNum < size2)
            {
                value = value * Div1 + z85Decoder[(byte)input[charNum]];
                value = value * Div1 + z85Decoder[(byte)input[charNum + 1]];
                value = value * Div1 + z85Decoder[(byte)input[charNum + 2]];
                value = value * Div1 + z85Decoder[(byte)input[charNum + 3]];
                value = value * Div1 + z85Decoder[(byte)input[charNum + 4]];
                charNum += 5;

                decoded[byteNum + 0] = (byte)(value >> 24);
                decoded[byteNum + 1] = (byte)(value >> 16);
                decoded[byteNum + 2] = (byte)(value >> 8);
                decoded[byteNum + 3] = (byte)value;
                byteNum += 4;
            }
        }

        value = 0;
        while (charNum < size)
            value = value * Div1 + Z85Map.Decoder[(byte)data[charNum++]];

        // Take care of the remainder.
        var divisor = (uint)Math.Pow(Byte1, extraBytes - 1);
        while (divisor != 0)
        {
            decoded[byteNum++] = (byte)(value / divisor % Byte1);
            divisor /= Byte1;
        }

        return decoded;
    }

    /// <summary>
    ///     Decodes a Z85 string into a byte array. The input length must be a multiple of 5.
    /// </summary>
    /// <param name="data"> The input Z85 string to decode</param>
    /// <returns> The decoded byte array</returns>
    /// <exception cref="ArgumentException"> Thrown when the input length is not a multiple of 5</exception>
    public static unsafe byte[] GetBytes(string data)
    {
        var size = data.Length;
        var decodedSize = GetDecodedSize(size);
        var decoded = new byte[decodedSize];
        var charNum = 0;
        var byteNum = 0;

        fixed (byte* z85Decoder = Z85Map.Decoder)
        fixed (char* input = data)
        {
            while (charNum < size)
            {
                uint value = 0;
                value = value * Div1 + z85Decoder[(byte)input[charNum]];
                value = value * Div1 + z85Decoder[(byte)input[charNum + 1]];
                value = value * Div1 + z85Decoder[(byte)input[charNum + 2]];
                value = value * Div1 + z85Decoder[(byte)input[charNum + 3]];
                value = value * Div1 + z85Decoder[(byte)input[charNum + 4]];
                charNum += 5;

                decoded[byteNum + 0] = (byte)(value >> 24);
                decoded[byteNum + 1] = (byte)(value >> 16);
                decoded[byteNum + 2] = (byte)(value >> 8);
                decoded[byteNum + 3] = (byte)value;
                byteNum += 4;
            }
        }

        return decoded;
    }

    private static int GetEncodedSize(int byteLength)
    {
        if (byteLength % 4 != 0)
            throw new ArgumentOutOfRangeException(nameof(byteLength), "Data length should be multiple of 4.");
        return byteLength * 5 / 4;
    }

    private static int GetDecodedSize(int stringLength)
    {
        if (stringLength % 5 != 0)
            throw new ArgumentOutOfRangeException(nameof(stringLength),
                "Length of encoded string should be multiple of 5.");

        return stringLength * 4 / 5;
    }
}

internal static class Z85Map
{
    public static readonly HashSet<char> EncoderHashSet =
    [
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
        'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't',
        'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D',
        'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N',
        'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X',
        'Y', 'Z', '.', '-', ':', '+', '=', '^', '!', '/',
        '*', '?', '&', '<', '>', '(', ')', '[', ']', '{',
        '}', '@', '%', '$', '#'
    ];

    public static readonly char[] Encoder =
    [
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
        'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't',
        'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D',
        'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N',
        'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X',
        'Y', 'Z', '.', '-', ':', '+', '=', '^', '!', '/',
        '*', '?', '&', '<', '>', '(', ')', '[', ']', '{',
        '}', '@', '%', '$', '#'
    ];

    public static readonly byte[] Decoder =
    [
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,

        0x00, 0x44, 0x00, 0x54, 0x53, 0x52, 0x48, 0x00,
        0x4B, 0x4C, 0x46, 0x41, 0x00, 0x3F, 0x3E, 0x45,
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
        0x08, 0x09, 0x40, 0x00, 0x49, 0x42, 0x4A, 0x47,
        0x51, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A,
        0x2B, 0x2C, 0x2D, 0x2E, 0x2F, 0x30, 0x31, 0x32,
        0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A,
        0x3B, 0x3C, 0x3D, 0x4D, 0x00, 0x4E, 0x43, 0x00,
        0x00, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
        0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
        0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20,
        0x21, 0x22, 0x23, 0x4F, 0x00, 0x50, 0x00, 0x00
    ];
}