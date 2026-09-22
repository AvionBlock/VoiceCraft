using VoiceCraft.Network.Packets.McApiPackets.Event;
using VoiceCraft.Network.Packets.McApiPackets.Request;
using VoiceCraft.Network.Packets.VcPackets.Event;
using VoiceCraft.Network.Packets.VcPackets.Request;
using Xunit;

namespace VoiceCraft.Network.Tests.Packets;

public class AudioPacketPoolingTests
{
    [Fact]
    public void VcAudioRequest_Return_ClearsBufferReference()
    {
        var buffer = new byte[] { 1, 2, 3 };
        var packet = new VcAudioRequestPacket();

        packet.Set(42, 0.5f, buffer.Length, buffer);
        packet.Return();

        Assert.NotSame(buffer, packet.Buffer);
        Assert.Empty(packet.Buffer);
        Assert.Equal(0, packet.Length);
        Assert.Equal(0, packet.Timestamp);
        Assert.Equal(0f, packet.FrameLoudness);
    }

    [Fact]
    public void McApiEntityAudioRequest_Return_ClearsBufferReference()
    {
        var buffer = new byte[] { 1, 2, 3 };
        var packet = new McApiEntityAudioRequestPacket();

        packet.Set(7, 42, 0.5f, buffer.Length, buffer);
        packet.Return();

        Assert.NotSame(buffer, packet.Buffer);
        Assert.Empty(packet.Buffer);
        Assert.Equal(0, packet.Length);
        Assert.Equal(0, packet.Id);
        Assert.Equal(0, packet.Timestamp);
        Assert.Equal(0f, packet.FrameLoudness);
    }

    [Fact]
    public void VcAudioDataEvent_Return_ClearsBufferReference()
    {
        var buffer = new byte[] { 1, 2, 3 };
        var packet = new VcOnEntityAudioDataReceivedPacket();

        packet.Set(7, 42, 0.5f, buffer.Length, buffer);
        packet.Return();

        Assert.NotSame(buffer, packet.Buffer);
        Assert.Empty(packet.Buffer);
        Assert.Equal(0, packet.Length);
        Assert.Equal(0, packet.Id);
        Assert.Equal(0, packet.Timestamp);
        Assert.Equal(0f, packet.FrameLoudness);
    }

    [Fact]
    public void McApiAudioDataEvent_Return_ClearsBufferReference()
    {
        var buffer = new byte[] { 1, 2, 3 };
        var packet = new McApiOnEntityAudioDataReceivedPacket();

        packet.Set(7, 42, 0.5f, buffer.Length, buffer);
        packet.Return();

        Assert.NotSame(buffer, packet.Buffer);
        Assert.Empty(packet.Buffer);
        Assert.Equal(0, packet.Length);
        Assert.Equal(0, packet.Id);
        Assert.Equal(0, packet.Timestamp);
        Assert.Equal(0f, packet.FrameLoudness);
    }
}
