using Xunit;

namespace VoiceCraft.Network.Tests.Codecs;

public class McWssPacketFramingTests
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

        var packed = McWssPacketFraming.Pack(packets);
        var unpacked = McWssPacketFraming.Unpack(packed);

        Assert.Equal(packets, unpacked);
    }

    [Fact]
    public void TryAppendFrame_Stops_WhenFrameWouldExceedMaxLength()
    {
        var builder = new System.Text.StringBuilder();

        var first = McWssPacketFraming.TryAppendFrame(builder, "abcd", 14, allowOversizedFirstFrame: false);
        var second = McWssPacketFraming.TryAppendFrame(builder, "efghijkl", 14, allowOversizedFirstFrame: false);

        Assert.True(first);
        Assert.False(second);
        Assert.Equal(["abcd"], McWssPacketFraming.Unpack(builder.ToString()));
    }

    [Fact]
    public void TryAppendFrame_AllowsOversizedFirstFrame_WhenConfigured()
    {
        var builder = new System.Text.StringBuilder();

        var appended = McWssPacketFraming.TryAppendFrame(builder, "abcdefghijklmnop", 5, allowOversizedFirstFrame: true);

        Assert.True(appended);
        Assert.Equal(["abcdefghijklmnop"], McWssPacketFraming.Unpack(builder.ToString()));
    }

    [Fact]
    public void Unpack_Rejects_MalformedFrames()
    {
        Assert.Throws<ArgumentException>(() => McWssPacketFraming.Unpack("abc"));
        Assert.Throws<ArgumentException>(() => McWssPacketFraming.Unpack("|x"));
        Assert.Throws<ArgumentException>(() => McWssPacketFraming.Unpack("00short"));
    }

    [Fact]
    public void Pack_UsesCompactTwoCharacterHeader()
    {
        var packed = McWssPacketFraming.Pack(["abcd"]);

        Assert.Equal(6, packed.Length);
        Assert.Equal("abcd", McWssPacketFraming.Unpack(packed).Single());
    }
}
