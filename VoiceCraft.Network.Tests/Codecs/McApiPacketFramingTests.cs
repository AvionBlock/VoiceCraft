using Xunit;

namespace VoiceCraft.Network.Tests.Codecs;

public class McApiPacketFramingTests
{
    [Fact]
    public void PackUnpack_RoundTrips_MultiplePackets()
    {
        var packets = new[]
        {
            "abc123",
            "payload:with:colon",
            string.Empty,
            "ZZ-top"
        };

        var packed = McApiPacketFraming.Pack(packets);
        var unpacked = McApiPacketFraming.Unpack(packed);

        Assert.Equal(packets, unpacked);
    }

    [Fact]
    public void TryAppendFrame_Stops_WhenFrameWouldExceedMaxLength()
    {
        var builder = new System.Text.StringBuilder();

        var first = McApiPacketFraming.TryAppendFrame(builder, "abcd", 14, allowOversizedFirstFrame: false);
        var second = McApiPacketFraming.TryAppendFrame(builder, "efghijkl", 14, allowOversizedFirstFrame: false);

        Assert.True(first);
        Assert.False(second);
        Assert.Equal(["abcd"], McApiPacketFraming.Unpack(builder.ToString()));
    }

    [Fact]
    public void TryAppendFrame_AllowsOversizedFirstFrame_WhenConfigured()
    {
        var builder = new System.Text.StringBuilder();

        var appended = McApiPacketFraming.TryAppendFrame(builder, "abcdefghijklmnop", 5, allowOversizedFirstFrame: true);

        Assert.True(appended);
        Assert.Equal(["abcdefghijklmnop"], McApiPacketFraming.Unpack(builder.ToString()));
    }

    [Fact]
    public void Unpack_Rejects_MalformedFrames()
    {
        Assert.Throws<ArgumentException>(() => McApiPacketFraming.Unpack("abc"));
        Assert.Throws<ArgumentException>(() => McApiPacketFraming.Unpack("|x"));
        Assert.Throws<ArgumentException>(() => McApiPacketFraming.Unpack("00short"));
    }

    [Fact]
    public void Pack_UsesCompactTwoCharacterHeader()
    {
        var packed = McApiPacketFraming.Pack(["abcd"]);

        Assert.Equal(6, packed.Length);
        Assert.Equal("abcd", McApiPacketFraming.Unpack(packed).Single());
    }
}
