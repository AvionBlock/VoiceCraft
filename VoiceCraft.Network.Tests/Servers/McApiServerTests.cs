using System.Collections.Immutable;
using System.Numerics;
using VoiceCraft.Core.World;
using VoiceCraft.Network.NetPeers;
using VoiceCraft.Network.Packets.McApiPackets;
using VoiceCraft.Network.Packets.McApiPackets.Request;
using VoiceCraft.Network.Packets.McApiPackets.Response;
using VoiceCraft.Network.Servers;
using VoiceCraft.Network.Systems;
using Xunit;

namespace VoiceCraft.Network.Tests.Servers;

public class McApiServerTests
{
    [Fact]
    public void CreateEntityRequest_AddsEntityToWorld()
    {
        using var world = new VoiceCraftWorld();
        using var effectSystem = new AudioEffectSystem();
        var server = new TestMcApiServer(world, effectSystem);
        var peer = new HttpMcApiNetPeer(null)
        {
            ConnectionState = McApiConnectionState.Connected
        };
        peer.SetSessionToken("session-token");

        var request = new McApiCreateEntityRequestPacket();
        request.Set(requestId: "create-1");

        server.Dispatch(request, peer);

        var response = Assert.IsType<McApiCreateEntityResponsePacket>(server.LastPacket);
        Assert.Equal(McApiCreateEntityResponsePacket.ResponseCodes.Ok, response.ResponseCode);

        var entity = world.GetEntity(response.Id);
        Assert.NotNull(entity);
        Assert.Equal("world", entity.WorldId);
        Assert.Equal("Created Entity", entity.Name);
        Assert.True(entity.Muted);
        Assert.Equal((ushort)3, entity.TalkBitmask);
        Assert.Equal((ushort)5, entity.ListenBitmask);
        Assert.Equal((ushort)7, entity.EffectBitmask);
        Assert.Equal(new Vector3(1, 2, 3), entity.Position);
        Assert.Equal(new Vector2(4, 5), entity.Rotation);
    }

    private sealed class TestMcApiServer(VoiceCraftWorld world, AudioEffectSystem audioEffectSystem)
        : McApiServer(world, audioEffectSystem)
    {
        public IMcApiPacket? LastPacket { get; private set; }

        public override string LoginToken => string.Empty;
        public override uint MaxClients => 10;
        public override int ConnectedPeers => 1;
        public override ImmutableList<McApiNetPeer> Peers => ImmutableList<McApiNetPeer>.Empty;

        public override event Action<McApiNetPeer, string>? OnPeerConnected;
        public override event Action<McApiNetPeer, string>? OnPeerDisconnected;

        public void Dispatch(IMcApiPacket packet, McApiNetPeer netPeer)
        {
            ExecutePacket(packet, netPeer);
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

        public override void SendPacket<T>(McApiNetPeer netPeer, T packet)
        {
            LastPacket = packet;
        }

        public override void Broadcast<T>(T packet, params McApiNetPeer?[] excludes)
        {
        }

        public override void Disconnect(McApiNetPeer netPeer, bool force = false)
        {
        }

        protected override void AcceptRequest(McApiLoginRequestPacket packet, McApiNetPeer netPeer)
        {
            OnPeerConnected?.Invoke(netPeer, string.Empty);
        }

        protected override void RejectRequest(McApiLoginRequestPacket packet, string reason, McApiNetPeer netPeer)
        {
            OnPeerDisconnected?.Invoke(netPeer, reason);
        }
    }
}
