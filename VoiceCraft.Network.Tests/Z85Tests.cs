using LiteNetLib.Utils;
using VoiceCraft.Network.Packets.McApiPackets.Request;
using Xunit;
using Xunit.Abstractions;

namespace VoiceCraft.Network.Tests;

public class Z85Tests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public Z85Tests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void GetString_WithUnpaddedLength_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Z85.GetString([1, 2, 3]));
    }

    [Fact]
    public void Z85EncodeDecoder_Is_Same()
    {
        byte[] bytes = [50, 205, 25, 56];
        var encoded = Z85.GetString(bytes);
        var decoded = Z85.GetBytes(encoded);
        Assert.Equal(bytes, decoded);
    }
    
    [Fact]
    public void Z85EncodeDecoder_With_Padding_Is_Same()
    {
        byte[] bytes = [50, 205, 25, 56, 7];
        var encoded = Z85.GetStringWithPadding(bytes);
        var decoded = Z85.GetBytesWithPadding(encoded);
        Assert.Equal(bytes, decoded);
    }

    [Fact]
    public void LoginPacketTest()
    {
        var packet = new McApiLoginRequestPacket("testAAA", "test2AAAA", new Version(1, 7, 0), []);
        var writer = new NetDataWriter();
        var reader = new NetDataReader();
        packet.Serialize(writer);
        var encoded = writer.CopyData();
        var z85Encoded = Z85.GetStringWithPadding(encoded);
        var z85Decoded = Z85.GetBytesWithPadding(z85Encoded);
        reader.SetSource(z85Decoded);
        packet.Deserialize(reader);
        _testOutputHelper.WriteLine(z85Encoded);
    }
}
