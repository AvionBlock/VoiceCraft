using Xunit;

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
            Assert.True(McApiStringCodec.IsSafePayloadCharacter(ch));
            Assert.NotEqual('|', ch);
            Assert.NotEqual('"', ch);
            Assert.NotEqual('\\', ch);
            Assert.NotEqual('%', ch);
            Assert.NotEqual(' ', ch);
            Assert.False(ch < 0x20);
            Assert.NotEqual(0x7F, ch);
            Assert.True(ch <= 0x7E);
        });
    }

    [Fact]
    public void Decode_Rejects_InvalidCharacters()
    {
        Assert.Throws<ArgumentException>(() => McApiStringCodec.Decode("|"));
        Assert.Throws<ArgumentException>(() => McApiStringCodec.Decode("\""));
        Assert.Throws<ArgumentException>(() => McApiStringCodec.Decode("\\"));
        Assert.Throws<ArgumentException>(() => McApiStringCodec.Decode("%"));
        Assert.Throws<ArgumentException>(() => McApiStringCodec.Decode(" "));
    }

    [Fact]
    public void EncodeDecode_Preserves_LeadingZeroBytes()
    {
        var data = new byte[] { 0, 0, 0, 1, 2, 3, 0, 4 };
        var encoded = McApiStringCodec.Encode(data);
        var decoded = McApiStringCodec.Decode(encoded);

        Assert.Equal(data, decoded);
    }
}
