using System;
using System.Collections.Generic;
using System.Text;

namespace VoiceCraft.Network;

public static class McWssPacketFraming
{
    private const int HeaderSize = 2;
    private static readonly int MaxPacketLength = McApiStringCodec.AlphabetSize * McApiStringCodec.AlphabetSize - 1;

    public static string Pack(IEnumerable<string> packets)
    {
        var builder = new StringBuilder();
        foreach (var packet in packets)
            AppendFrame(builder, packet);
        return builder.ToString();
    }

    public static bool TryAppendFrame(StringBuilder builder, string packet, int maxLength, bool allowOversizedFirstFrame)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(packet);

        ValidatePacketLength(packet);
        var frameLength = GetFrameLength(packet);
        switch (builder.Length)
        {
            case > 0 when builder.Length + frameLength > maxLength:
            case 0 when !allowOversizedFirstFrame && frameLength > maxLength:
                return false;
            default:
                AppendFrame(builder, packet);
                return true;
        }
    }

    public static List<string> Unpack(string data)
    {
        var packets = new List<string>();
        if (string.IsNullOrEmpty(data))
            return packets;

        var index = 0;
        while (index < data.Length)
        {
            if (data.Length - index < HeaderSize)
                throw new ArgumentException("Packet frame is truncated and missing its length header.", nameof(data));

            var length = DecodeLength(data[index], data[index + 1]);
            index += HeaderSize;
            if (index + length > data.Length)
                throw new ArgumentException("Packet frame length exceeds the available payload.", nameof(data));

            packets.Add(data.Substring(index, length));
            index += length;
        }

        return packets;
    }

    private static void AppendFrame(StringBuilder builder, string packet)
    {
        ValidatePacketLength(packet);
        var (high, low) = EncodeLength(packet.Length);
        builder.Append(high);
        builder.Append(low);
        builder.Append(packet);
    }

    private static int GetFrameLength(string packet)
    {
        return packet.Length + HeaderSize;
    }

    private static (char High, char Low) EncodeLength(int length)
    {
        var baseSize = McApiStringCodec.AlphabetSize;
        return (
            McApiStringCodec.GetAlphabetChar(length / baseSize),
            McApiStringCodec.GetAlphabetChar(length % baseSize));
    }

    private static int DecodeLength(char high, char low)
    {
        var baseSize = McApiStringCodec.AlphabetSize;
        return McApiStringCodec.GetAlphabetIndex(high) * baseSize +
               McApiStringCodec.GetAlphabetIndex(low);
    }

    private static void ValidatePacketLength(string packet)
    {
        if (packet.Length > MaxPacketLength)
            throw new ArgumentOutOfRangeException(nameof(packet),
                $"Packet length {packet.Length} exceeds the McWss frame limit of {MaxPacketLength} characters.");
    }
}
