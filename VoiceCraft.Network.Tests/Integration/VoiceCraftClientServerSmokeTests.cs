using System.Net;
using System.Net.Sockets;
using System.Numerics;
using Xunit;
using VoiceCraft.Core.World;
using VoiceCraft.Network;
using VoiceCraft.Network.Clients;
using VoiceCraft.Network.Servers;
using VoiceCraft.Network.Systems;
using VoiceCraft.Network.World;
using VoiceCraft.Server.Systems;

namespace VoiceCraft.Network.Tests.Integration;

public class VoiceCraftClientServerSmokeTests
{
    [Fact]
    public async Task PingAsync_Returns_ServerInfo()
    {
        using var world = new VoiceCraftWorld();
        using var server = CreateServer(world, out var port);
        using var client = CreateClient();

        server.Start();

        var pingTask = client.PingAsync(IPAddress.Loopback.ToString(), port);
        await PumpUntilCompletedAsync(pingTask, () =>
        {
            server.Update();
            client.Update();
        });

        var info = await pingTask;

        Assert.Equal(server.Config.Motd, info.Motd);
        Assert.Equal(0, info.Clients);
        Assert.Equal(server.Config.PositioningType, info.PositioningType);
    }

    [Fact]
    public async Task ConnectAndDisconnectAsync_UpdatesConnectionStateAndServerPeerCount()
    {
        using var world = new VoiceCraftWorld();
        using var server = CreateServer(world, out var port);
        using var client = CreateClient();

        server.Start();

        var connectTask = client.ConnectAsync(
            IPAddress.Loopback.ToString(),
            port,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "en-US",
            PositioningType.Client);

        await PumpUntilCompletedAsync(connectTask, () =>
        {
            server.Update();
            client.Update();
        });
        await connectTask;

        Assert.Equal(VcConnectionState.Connected, client.ConnectionState);
        Assert.Equal(1, server.ConnectedPeers);
        Assert.Single(server.WorldSnapshot.OfType<VoiceCraftNetworkEntity>());

        var disconnectTask = client.DisconnectAsync("test");
        await PumpUntilAsync(
            () => client.ConnectionState == VcConnectionState.Disconnected && server.ConnectedPeers == 0,
            () =>
            {
                server.Update();
                client.Update();
            });
        await disconnectTask;

        Assert.Equal(VcConnectionState.Disconnected, client.ConnectionState);
        Assert.Empty(server.WorldSnapshot);
    }

    [Fact]
    public async Task ClientUpdates_PropagateToServerEntity()
    {
        using var world = new VoiceCraftWorld();
        using var server = CreateServer(world, out var port);
        using var client = CreateClient();
        var userGuid = Guid.NewGuid();

        server.Start();

        var connectTask = client.ConnectAsync(
            IPAddress.Loopback.ToString(),
            port,
            userGuid,
            Guid.NewGuid(),
            "en-US",
            PositioningType.Client);

        await PumpUntilCompletedAsync(connectTask, () =>
        {
            server.Update();
            client.Update();
        });
        await connectTask;

        client.Name = "SmokeClient";
        client.Muted = true;
        client.Position = new Vector3(1, 2, 3);
        client.Rotation = new Vector2(4, 5);
        client.WorldId = "overworld";

        await PumpUntilAsync(
            () =>
            {
                var entity = GetServerEntity(server, userGuid);
                return entity is
                {
                    Name: "SmokeClient",
                    Muted: true,
                    WorldId: "overworld"
                } &&
                entity.Position == new Vector3(1, 2, 3) &&
                entity.Rotation == new Vector2(4, 5);
            },
            () =>
            {
                server.Update();
                client.Update();
            });

        var serverEntity = GetServerEntity(server, userGuid);
        Assert.NotNull(serverEntity);
        Assert.Equal("SmokeClient", serverEntity.Name);
        Assert.True(serverEntity.Muted);
        Assert.Equal("overworld", serverEntity.WorldId);
        Assert.Equal(new Vector3(1, 2, 3), serverEntity.Position);
        Assert.Equal(new Vector2(4, 5), serverEntity.Rotation);
    }

    [Fact]
    public async Task ConnectedClients_ReceiveEachOthersEntitiesAndNameUpdates()
    {
        using var world = new VoiceCraftWorld();
        using var server = CreateServer(world, out var port);
        using var audioEffectSystem = new AudioEffectSystem();
        using var eventHandlerSystem = new EventHandlerSystem(
            server,
            Array.Empty<McApiServer>(),
            audioEffectSystem,
            world);
        var visibilitySystem = new VisibilitySystem(world, audioEffectSystem);
        using var clientA = CreateClient();
        using var clientB = CreateClient();
        var userGuidA = Guid.NewGuid();
        var userGuidB = Guid.NewGuid();

        server.Start();

        var connectTaskA = clientA.ConnectAsync(
            IPAddress.Loopback.ToString(),
            port,
            userGuidA,
            Guid.NewGuid(),
            "en-US",
            PositioningType.Client);

        await PumpUntilCompletedAsync(connectTaskA, () =>
        {
            server.Update();
            visibilitySystem.Update();
            eventHandlerSystem.Update();
            clientA.Update();
        });
        await connectTaskA;

        var connectTaskB = clientB.ConnectAsync(
            IPAddress.Loopback.ToString(),
            port,
            userGuidB,
            Guid.NewGuid(),
            "en-US",
            PositioningType.Client);

        await PumpUntilCompletedAsync(connectTaskB, () =>
        {
            server.Update();
            visibilitySystem.Update();
            eventHandlerSystem.Update();
            clientA.Update();
            clientB.Update();
        });
        await connectTaskB;

        await PumpUntilAsync(
            () =>
                clientA.World.Entities.OfType<VoiceCraftClientNetworkEntity>().Any(x => x.UserGuid == userGuidB) &&
                clientB.World.Entities.OfType<VoiceCraftClientNetworkEntity>().Any(x => x.UserGuid == userGuidA),
            () =>
            {
                server.Update();
                visibilitySystem.Update();
                eventHandlerSystem.Update();
                clientA.Update();
                clientB.Update();
            });

        clientA.Name = "Alpha";
        await PumpUntilAsync(
            () => clientB.World.Entities.OfType<VoiceCraftClientNetworkEntity>()
                .Any(x => x.UserGuid == userGuidA && x.Name == "Alpha"),
            () =>
            {
                server.Update();
                visibilitySystem.Update();
                eventHandlerSystem.Update();
                clientA.Update();
                clientB.Update();
            });

        Assert.Contains(clientA.World.Entities.OfType<VoiceCraftClientNetworkEntity>(), x => x.UserGuid == userGuidB);
        Assert.Contains(clientB.World.Entities.OfType<VoiceCraftClientNetworkEntity>(),
            x => x.UserGuid == userGuidA && x.Name == "Alpha");
    }

    private static async Task PumpUntilCompletedAsync(Task task, params Action[] pumps)
    {
        await PumpUntilAsync(() => task.IsCompleted, pumps);
    }

    private static async Task PumpUntilAsync(Func<bool> condition, params Action[] pumps)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        while (!condition())
        {
            cts.Token.ThrowIfCancellationRequested();
            foreach (var pump in pumps)
                pump();
            await Task.Delay(10, cts.Token);
        }
    }

    private static TestLiteNetVoiceCraftServer CreateServer(VoiceCraftWorld world, out int port)
    {
        port = GetFreeUdpPort();
        var server = new TestLiteNetVoiceCraftServer(world)
        {
            Config = new LiteNetVoiceCraftServer.LiteNetVoiceCraftConfig
            {
                Port = (uint)port,
                MaxClients = 8,
                Motd = "VoiceCraft Test Server",
                PositioningType = PositioningType.Client
            }
        };

        return server;
    }

    private static LiteNetVoiceCraftClient CreateClient()
    {
        return new LiteNetVoiceCraftClient(new FakeAudioEncoder(), () => new FakeAudioDecoder());
    }

    private static VoiceCraftNetworkEntity GetServerEntity(TestLiteNetVoiceCraftServer server, Guid userGuid)
    {
        return Assert.Single(server.WorldSnapshot.OfType<VoiceCraftNetworkEntity>(), x => x.UserGuid == userGuid);
    }

    private static int GetFreeUdpPort()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)socket.LocalEndPoint!).Port;
    }

    private sealed class TestLiteNetVoiceCraftServer : LiteNetVoiceCraftServer
    {
        private readonly VoiceCraftWorld _world;

        public TestLiteNetVoiceCraftServer(VoiceCraftWorld world) : base(world)
        {
            _world = world;
        }

        public IReadOnlyCollection<VoiceCraftEntity> WorldSnapshot => _world.Entities.ToArray();
    }

    private sealed class FakeAudioEncoder : VoiceCraft.Core.Interfaces.IAudioEncoder
    {
        public int Encode(Span<float> data, Span<byte> output, int samples)
        {
            var written = Math.Min(samples, Math.Min(data.Length, output.Length));
            for (var i = 0; i < written; i++)
                output[i] = (byte)(Math.Clamp(data[i], -1f, 1f) * 127 + 128);
            return written;
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeAudioDecoder : VoiceCraft.Core.Interfaces.IAudioDecoder
    {
        public int Decode(Span<byte> buffer, Span<float> output, int samples)
        {
            var read = Math.Min(samples, Math.Min(buffer.Length, output.Length));
            for (var i = 0; i < read; i++)
                output[i] = (buffer[i] - 128) / 127f;
            return read;
        }

        public void Dispose()
        {
        }
    }
}
