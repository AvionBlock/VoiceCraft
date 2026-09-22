using System;
using LiteNetLib.Utils;
using VoiceCraft.Network.Packets.McApiPackets.Request;
using Xunit;

namespace VoiceCraft.Network.Tests;

public class Z85Tests
{
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
    public void LoginPacket_SurvivesZ85Transport()
    {
        var packet = new McApiLoginRequestPacket("request-42", "token-123", new Version(1, 7, 0),
            [EventType.OnEntityCreated, EventType.OnEntityDestroyed]);
        var writer = new NetDataWriter();
        var reader = new NetDataReader();
        packet.Serialize(writer);
        var encoded = writer.CopyData();
        var z85Encoded = Z85.GetStringWithPadding(encoded);
        var z85Decoded = Z85.GetBytesWithPadding(z85Encoded);
        reader.SetSource(z85Decoded);
        var clone = new McApiLoginRequestPacket();
        clone.Deserialize(reader);

        Assert.Equal(packet.RequestId, clone.RequestId);
        Assert.Equal(packet.Token, clone.Token);
        Assert.Equal(packet.Version, clone.Version);
        Assert.Equal(packet.SubscribeEvents, clone.SubscribeEvents);
        Assert.True(reader.EndOfData);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(16)]
    [InlineData(31)]
    [InlineData(64)]
    public void GetBytesWithPadding_RoundTripsVariableLengthPayloads(int length)
    {
        var bytes = new byte[length];
        new Random(42).NextBytes(bytes);

        Assert.Equal(bytes, Z85.GetBytesWithPadding(Z85.GetStringWithPadding(bytes)));
    }

    [Theory]
    [InlineData("0000")]
    [InlineData("000000")]
    [InlineData("000004")]
    [InlineData("00000x")]
    public void GetBytesWithPadding_RejectsInvalidLengthOrPadding(string encoded)
    {
        Assert.Throws<ArgumentException>(() => Z85.GetBytesWithPadding(encoded));
    }
}
