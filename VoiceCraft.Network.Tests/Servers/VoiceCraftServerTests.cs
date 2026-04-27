using System.Net;
using Xunit;
using VoiceCraft.Core.World;
using VoiceCraft.Network.NetPeers;
using VoiceCraft.Network.Packets.VcPackets;
using VoiceCraft.Network.Packets.VcPackets.Request;
using VoiceCraft.Network.Packets.VcPackets.Response;
using VoiceCraft.Network.Servers;

namespace VoiceCraft.Network.Tests.Servers;

public class VoiceCraftServerTests
{
    [Fact]
    public void InfoRequest_SendsInfoResponse()
    {
        using var world = new VoiceCraftWorld();
        var server = new TestVoiceCraftServer(world);
        var endpoint = new IPEndPoint(IPAddress.Loopback, 9050);

        server.Dispatch(PacketPool<VcInfoRequestPacket>.GetPacket(() => new VcInfoRequestPacket()).Set(123), endpoint);

        var response = Assert.IsType<VcInfoResponsePacket>(server.LastUnconnectedPacket);
        Assert.Equal(server.Motd, response.Motd);
        Assert.Equal(server.PositioningType, response.PositioningType);
        Assert.Equal(123, response.Tick);
    }

    [Fact]
    public void LoginRequest_WithIncompatibleVersion_IsRejected()
    {
        using var world = new VoiceCraftWorld();
        var server = new TestVoiceCraftServer(world);
        var packet = new VcLoginRequestPacket().Set(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "en-US",
            new Version(999, 0, 0),
            PositioningType.Client);

        server.Dispatch(packet, new object());

        Assert.False(server.Accepted);
        Assert.Equal("VoiceCraft.DisconnectReason.IncompatibleVersion", server.LastRejectedReason);
    }

    [Fact]
    public void LoginRequest_WhenServerFull_IsRejected()
    {
        using var world = new VoiceCraftWorld();
        var server = new TestVoiceCraftServer(world)
        {
            SimulatedConnectedPeers = 1,
            SimulatedMaxClients = 1
        };
        var packet = CreateValidLoginPacket(PositioningType.Client);

        server.Dispatch(packet, new object());

        Assert.False(server.Accepted);
        Assert.Equal("VoiceCraft.DisconnectReason.ServerFull", server.LastRejectedReason);
    }

    [Fact]
    public void LoginRequest_WhenPositioningTypeMismatches_IsRejected()
    {
        using var world = new VoiceCraftWorld();
        var server = new TestVoiceCraftServer(world)
        {
            SimulatedPositioningType = PositioningType.Server
        };
        var packet = CreateValidLoginPacket(PositioningType.Client);

        server.Dispatch(packet, new object());

        Assert.False(server.Accepted);
        Assert.Equal("VoiceCraft.DisconnectReason.ServerSidedOnly", server.LastRejectedReason);
    }

    [Fact]
    public void LoginRequest_WhenValid_IsAccepted()
    {
        using var world = new VoiceCraftWorld();
        var server = new TestVoiceCraftServer(world);
        var marker = new object();
        var packet = CreateValidLoginPacket(PositioningType.Client);

        server.Dispatch(packet, marker);

        Assert.True(server.Accepted);
        Assert.Same(marker, server.AcceptedData);
        Assert.Null(server.LastRejectedReason);
    }

    private static VcLoginRequestPacket CreateValidLoginPacket(PositioningType positioningType)
    {
        return new VcLoginRequestPacket().Set(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "en-US",
            VoiceCraftServer.Version,
            positioningType);
    }

    private sealed class TestVoiceCraftServer(VoiceCraftWorld world) : VoiceCraftServer(world)
    {
        public bool Accepted { get; private set; }
        public object? AcceptedData { get; private set; }
        public string? LastRejectedReason { get; private set; }
        public IVoiceCraftPacket? LastUnconnectedPacket { get; private set; }
        public int SimulatedConnectedPeers { get; set; }
        public uint SimulatedMaxClients { get; set; } = 10;
        public PositioningType SimulatedPositioningType { get; set; } = PositioningType.Client;

        public override string Motd => "Test Motd";
        public override PositioningType PositioningType => SimulatedPositioningType;
        public override uint MaxClients => SimulatedMaxClients;
        public override int ConnectedPeers => SimulatedConnectedPeers;

        public void Dispatch(IVoiceCraftPacket packet, object? data)
        {
            ExecutePacket(packet, data);
        }

        public override void Start()
        {
        }

        public override void Update()
        {
        }

        public override void Stop()
        {
        }

        public override void SendUnconnectedPacket<T>(IPEndPoint endPoint, T packet)
        {
            LastUnconnectedPacket = packet;
            PacketPool<T>.Return(packet);
        }

        public override void SendPacket<T>(VoiceCraftNetPeer vcNetPeer, T packet, VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable)
        {
            PacketPool<T>.Return(packet);
        }

        public override void Broadcast<T>(T packet, VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable, params VoiceCraftNetPeer?[] excludes)
        {
            PacketPool<T>.Return(packet);
        }

        public override void Disconnect(VoiceCraftNetPeer vcNetPeer, string reason, bool force = false)
        {
        }

        public override void DisconnectAll(string? reason = null)
        {
        }

        protected override void AcceptRequest(VcLoginRequestPacket packet, object? data)
        {
            Accepted = true;
            AcceptedData = data;
        }

        protected override void RejectRequest(VcLoginRequestPacket packet, string reason, object? data)
        {
            LastRejectedReason = reason;
        }
    }
}
