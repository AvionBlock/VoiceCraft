using System.Numerics;
using LiteNetLib.Utils;
using Xunit;
using VoiceCraft.Network.Packets.VcPackets;
using VoiceCraft.Network.Packets.VcPackets.Event;
using VoiceCraft.Network.Packets.VcPackets.Request;
using VoiceCraft.Network.Packets.VcPackets.Response;

namespace VoiceCraft.Network.Tests.Packets;

public class PacketSerializationTests
{
    [Fact]
    public void LoginRequest_RoundTrips()
    {
        var requestId = Guid.NewGuid();
        var userGuid = Guid.NewGuid();
        var serverUserGuid = Guid.NewGuid();
        var packet = new VcLoginRequestPacket().Set(
            requestId,
            userGuid,
            serverUserGuid,
            "en-US",
            new Version(1, 5, 1),
            PositioningType.Client);

        var clone = RoundTrip(packet, () => new VcLoginRequestPacket());

        Assert.Equal(requestId, clone.RequestId);
        Assert.Equal(userGuid, clone.UserGuid);
        Assert.Equal(serverUserGuid, clone.ServerUserGuid);
        Assert.Equal("en-US", clone.Locale);
        Assert.Equal(new Version(1, 5, 1), clone.Version);
        Assert.Equal(PositioningType.Client, clone.PositioningType);
    }

    [Fact]
    public void InfoResponse_RoundTrips()
    {
        var packet = new VcInfoResponsePacket().Set(
            "Test MOTD",
            7,
            PositioningType.Server,
            1234,
            new Version(1, 5, 1));

        var clone = RoundTrip(packet, () => new VcInfoResponsePacket());

        Assert.Equal("Test MOTD", clone.Motd);
        Assert.Equal(7, clone.Clients);
        Assert.Equal(PositioningType.Server, clone.PositioningType);
        Assert.Equal(1234, clone.Tick);
        Assert.Equal(new Version(1, 5, 1), clone.Version);
    }

    [Fact]
    public void AudioRequest_RoundTrips()
    {
        var payload = new byte[] { 1, 2, 3, 4, 5 };
        var packet = new VcAudioRequestPacket().Set(77, 0.5f, payload.Length, payload);

        var clone = RoundTrip(packet, () => new VcAudioRequestPacket());

        Assert.Equal((ushort)77, clone.Timestamp);
        Assert.Equal(0.5f, clone.FrameLoudness, 3);
        Assert.Equal(payload.Length, clone.Length);
        Assert.Equal(payload, clone.Buffer);
    }

    [Fact]
    public void SetPositionRequest_RoundTrips()
    {
        var packet = new VcSetPositionRequestPacket().Set(new Vector3(1.25f, 2.5f, 3.75f));

        var clone = RoundTrip(packet, () => new VcSetPositionRequestPacket());

        Assert.Equal(new Vector3(1.25f, 2.5f, 3.75f), clone.Value);
    }

    [Fact]
    public void NetworkEntityCreatedEvent_RoundTrips()
    {
        var userGuid = Guid.NewGuid();
        var packet = new VcOnNetworkEntityCreatedPacket()
            .Set(42, "Alpha", true, false, userGuid, true, false);

        var clone = RoundTrip(packet, () => new VcOnNetworkEntityCreatedPacket());

        Assert.Equal(42, clone.Id);
        Assert.Equal("Alpha", clone.Name);
        Assert.True(clone.Muted);
        Assert.False(clone.Deafened);
        Assert.Equal(userGuid, clone.UserGuid);
        Assert.True(clone.ServerMuted);
        Assert.False(clone.ServerDeafened);
    }

    [Fact]
    public void EntityDestroyedEvent_RoundTrips()
    {
        var packet = new VcOnEntityDestroyedPacket().Set(99);

        var clone = RoundTrip(packet, () => new VcOnEntityDestroyedPacket());

        Assert.Equal(99, clone.Id);
    }

    private static T RoundTrip<T>(T packet, Func<T> factory) where T : class, IVoiceCraftPacket
    {
        var writer = new NetDataWriter();
        packet.Serialize(writer);

        var reader = new NetDataReader();
        reader.SetSource(writer.CopyData());

        var clone = factory();
        clone.Deserialize(reader);
        return clone;
    }
}
