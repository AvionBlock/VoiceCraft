using System.Text.Json;
using Xunit;
using VoiceCraft.Network.Packets.McHttpPackets;

namespace VoiceCraft.Network.Tests.Codecs;

public class McApiStringCodecTests
{
    [Fact]
    public void EncodeDecode_RoundTrips_ByteSequences()
    {
        var rng = new Random(1337);

        foreach (var length in Enumerable.Range(0, 257))
        {
            var data = new byte[length];
            rng.NextBytes(data);

            var encoded = McApiStringCodec.Encode(data);
            var decoded = McApiStringCodec.Decode(encoded);

            Assert.Equal(data, decoded);
        }
    }

    [Fact]
    public void Encode_Uses_OnlyMcApiSafeCharacters()
    {
        var data = Enumerable.Range(0, ushort.MaxValue + 1)
            .SelectMany(value => new[] { (byte)(value >> 8), (byte)value })
            .ToArray();

        var encoded = McApiStringCodec.Encode(data);

        Assert.All(encoded, ch =>
        {
            Assert.False(ch == '|');
            Assert.False(ch == '"');
            Assert.False(ch == '\\');
            Assert.False(ch < 0x20);
            Assert.False(ch == 0x7F);
            Assert.False(char.IsSurrogate(ch));
        });
    }

    [Fact]
    public void Decode_Rejects_InvalidCharacters()
    {
        Assert.Throws<ArgumentException>(() => McApiStringCodec.Decode("|"));
        Assert.Throws<ArgumentException>(() => McApiStringCodec.Decode("\""));
        Assert.Throws<ArgumentException>(() => McApiStringCodec.Decode("\\"));
    }

    [Fact]
    public void Decode_Rejects_InvalidPaddingPlacement()
    {
        var invalid = "\uE000\uE001abc";

        Assert.Throws<ArgumentException>(() => McApiStringCodec.Decode(invalid));
    }

    [Fact]
    public void EncodedPacket_RoundTrips_ThroughJson()
    {
        var bytes = Enumerable.Range(0, 511).Select(i => (byte)(i % 256)).ToArray();
        var encoded = McApiStringCodec.Encode(bytes);
        var packet = new McHttpUpdatePacket
        {
            Packets = [encoded]
        };

        var json = JsonSerializer.Serialize(packet, McHttpUpdatePacketGenerationContext.Default.McHttpUpdatePacket);
        var roundTripped = JsonSerializer.Deserialize(json, McHttpUpdatePacketGenerationContext.Default.McHttpUpdatePacket);

        Assert.NotNull(roundTripped);
        Assert.Single(roundTripped!.Packets);
        Assert.Equal(encoded, roundTripped.Packets[0]);
        Assert.Equal(bytes, McApiStringCodec.Decode(roundTripped.Packets[0]));
    }
}
