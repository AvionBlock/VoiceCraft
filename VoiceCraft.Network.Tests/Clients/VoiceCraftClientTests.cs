using VoiceCraft.Core.Interfaces;
using VoiceCraft.Network.Clients;
using VoiceCraft.Network.Packets.VcPackets;
using VoiceCraft.Network.Packets.VcPackets.Event;
using Xunit;

namespace VoiceCraft.Network.Tests.Clients;

public class VoiceCraftClientTests
{
    [Fact]
    public void EntityCreated_WhenEntityAlreadyExists_IsIgnored()
    {
        using var client = new TestVoiceCraftClient();
        var packet = new VcOnEntityCreatedPacket();
        packet.Set(42, "First");

        client.Dispatch(packet);
        packet.Set(42, "Duplicate", true, true);
        client.Dispatch(packet);

        var entity = Assert.Single(client.World.Entities);
        Assert.Equal("First", entity.Name);
        Assert.False(entity.Muted);
        Assert.False(entity.Deafened);
    }

    [Fact]
    public void EntityDestroyed_WhenEntityDoesNotExist_IsIgnored()
    {
        using var client = new TestVoiceCraftClient();

        client.Dispatch(new VcOnEntityDestroyedPacket().Set(404));

        Assert.Empty(client.World.Entities);
    }

    private sealed class TestVoiceCraftClient() : VoiceCraftClient(new FakeAudioEncoder(), () => new FakeAudioDecoder())
    {
        public override PositioningType PositioningType => PositioningType.Client;
        public override event Action? OnConnected;
        public override event Action<string?>? OnDisconnected;

        public void Dispatch(IVoiceCraftPacket packet)
        {
            ExecutePacket(packet);
        }

        public override Task<ServerInfo> PingAsync(string ip, int port, CancellationToken token = default)
        {
            throw new NotSupportedException();
        }

        public override Task ConnectAsync(string ip, int port, Guid userGuid, Guid serverUserGuid, string locale,
            PositioningType positioningType)
        {
            OnConnected?.Invoke();
            return Task.CompletedTask;
        }

        public override void Update()
        {
        }

        public override Task DisconnectAsync(string? reason = null)
        {
            OnDisconnected?.Invoke(reason);
            return Task.CompletedTask;
        }

        public override void SendUnconnectedPacket<T>(string ip, int port, T packet)
        {
            PacketPool<T>.Return(packet);
        }

        public override void SendPacket<T>(T packet, VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable)
        {
            PacketPool<T>.Return(packet);
        }
    }

    private sealed class FakeAudioEncoder : IAudioEncoder
    {
        public int Encode(Span<float> data, Span<byte> output, int samples)
        {
            return 0;
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeAudioDecoder : IAudioDecoder
    {
        public int Decode(Span<byte> buffer, Span<float> output, int samples)
        {
            return 0;
        }

        public void Dispose()
        {
        }
    }
}
