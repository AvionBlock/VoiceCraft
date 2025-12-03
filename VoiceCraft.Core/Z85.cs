using System;
using System.Text;

namespace VoiceCraft.Core
{
    public static class Z85
    {
        private const int Base85 = 85;

        private static readonly char[] EncodingTable =
        {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
            'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't',
            'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D',
            'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N',
            'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X',
            'Y', 'Z', '.', '-', ':', '+', '=', '^', '!', '/',
            '*', '?', '&', '<', '>', '(', ')', '[', ']', '{',
            '}', '@', '%', '$', '#'
        };

        private static readonly uint[] DecodingTable =
        {
            0, 68, 0, 84, 83, 82, 72, 0,
            75, 76, 70, 65, 0, 63, 62, 69,
            0, 1, 2, 3, 4, 5, 6, 7,
            8, 9, 64, 0, 73, 66, 74, 71,
            81, 36, 37, 38, 39, 40, 41, 42,
            43, 44, 45, 46, 47, 48, 49, 50,
            51, 52, 53, 54, 55, 56, 57, 58,
            59, 60, 61, 77, 0, 78, 67, 0,
            0, 10, 11, 12, 13, 14, 15, 16,
            17, 18, 19, 20, 21, 22, 23, 24,
            25, 26, 27, 28, 29, 30, 31, 32,
            33, 34, 35, 79, 0, 80, 0, 0
        };

        /// <summary>
        ///     Encodes a byte array into a Z85 string with padding.
        /// </summary>
        /// <param name="data">The input byte array to encode</param>
        /// <returns> The Z85 encoded string</returns>
        /// <exception cref="ArgumentException"> Thrown when the input length is not a multiple of 4</exception>
        public static string GetStringWithPadding(Span<byte> data)
        {
            var lengthMod4 = data.Length % 4;
            var paddingRequired = lengthMod4 != 0;
            var bytesToEncode = data;
            var bytesToPad = 0;
            if (paddingRequired)
            {
                bytesToPad = 4 - lengthMod4;
                bytesToEncode = new byte[data.Length + bytesToPad];
                data.CopyTo(bytesToEncode);
            }

            var z85String = GetString(bytesToEncode);
            if (paddingRequired) z85String += bytesToPad;

            return z85String;
        }

        /// <summary>
        ///     Encodes a byte array into a Z85 string. The input length must be a multiple of 4.
        /// </summary>
        /// <param name="data">The input byte array to encode</param>
        /// <returns> The Z85 encoded string</returns>
        /// <exception cref="ArgumentException"> Thrown when the input length is not a multiple of 4</exception>
        public static string GetString(Span<byte> data)
        {
            var stringBuilder = new StringBuilder();
            var encodedChars = new char[5];

            for (var i = 0; i < data.Length; i += 4)
            {
                var binaryFrame = (uint)((data[i + 0] << 24) |
                                         (data[i + 1] << 16) |
                                         (data[i + 2] << 8) |
                                         data[i + 3]);
                
                var divisor = (uint)(Base85 * Base85 * Base85 * Base85);
                for (var j = 0; j < 5; j++)
                {
                    var divisible = binaryFrame / divisor % 85;
                    encodedChars[j] = EncodingTable[divisible];
                    binaryFrame -= divisible * divisor;
                    divisor /= Base85;
                }

                stringBuilder.Append(encodedChars);
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        ///     Decodes a Z85 string into a byte array. The input length must be a multiple of 5 (+ 1 with padding).
        /// </summary>
        /// <param name="data"> The input Z85 string to decode</param>
        /// <returns> The decoded byte array</returns>
        /// <exception cref="ArgumentException"> Thrown when the input length is not a multiple of 5 (+ 1 with padding).</exception>
        public static byte[] GetBytesWithPadding(string data)
        {
            var lengthMod5 = data.Length % 5;
            if (lengthMod5 != 0 && (data.Length - 1) % 5 != 0)
                throw new ArgumentException("Input length must be a multiple of 5 with either padding or no padding.",
                    nameof(data));

            var paddedBytes = 0;
            if (lengthMod5 != 0)
            {
                if (!int.TryParse(data[^1].ToString(), out paddedBytes)
                    || paddedBytes < 1
                    || paddedBytes > 3)
                    throw new ArgumentException("Invalid padding character for a Z85 string.");

                data = data.Remove(data.Length - 1);
            }

            var output = GetBytes(data);
            //Remove padded bytes
            if (paddedBytes > 0)
                Array.Resize(ref output, output.Length - paddedBytes);
            return output;
        }

        /// <summary>
        ///     Decodes a Z85 string into a byte array. The input length must be a multiple of 5.
        /// </summary>
        /// <param name="data"> The input Z85 string to decode</param>
        /// <returns> The decoded byte array</returns>
        /// <exception cref="ArgumentException"> Thrown when the input length is not a multiple of 5</exception>
        public static byte[] GetBytes(string data)
        {
            if (data.Length % 5 != 0) throw new ArgumentException("Input length must be a multiple of 5", nameof(data));

            var output = new byte[data.Length / 5 * 4];
            var outputIndex = 0;
            for (var i = 0; i < data.Length; i += 5)
            {
                uint value = 0;
                value = value * Base85 + DecodingTable[data[i] - 32];
                value = value * Base85 + DecodingTable[data[i + 1] - 32];
                value = value * Base85 + DecodingTable[data[i + 2] - 32];
                value = value * Base85 + DecodingTable[data[i + 3] - 32];
                value = value * Base85 + DecodingTable[data[i + 4] - 32];

                output[outputIndex] = (byte)(value >> 24);
                output[outputIndex + 1] = (byte)(value >> 16);
                output[outputIndex + 2] = (byte)(value >> 8);
                output[outputIndex + 3] = (byte)value;
                outputIndex += 4;
            }

            return output;
        }
    }
}