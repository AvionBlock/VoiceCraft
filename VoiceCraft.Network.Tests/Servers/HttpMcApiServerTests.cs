using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using LiteNetLib.Utils;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Packets.McApiPackets;
using VoiceCraft.Network.Packets.McApiPackets.Request;
using VoiceCraft.Network.Servers;
using VoiceCraft.Network.Systems;
using Xunit;

namespace VoiceCraft.Network.Tests.Servers;

public class HttpMcApiServerTests
{
    [Fact]
    public async Task ConnectResponse_ContainsOnlyServerPackets()
    {
        using var world = new VoiceCraftWorld();
        using var effects = new AudioEffectSystem();
        using var server = CreateServer(world, effects, out var baseAddress);
        using var client = new HttpClient();
        server.Start();
        var login = new McApiLoginRequestPacket();
        login.Set("request-1", "login-token", McApiServer.Version, []);

        var responseTask = client.PostAsync(
            new Uri(baseAddress, "connect"),
            new StringContent(Pack(login), Encoding.UTF8, "text/plain"));
        await PumpUntilCompletedAsync(responseTask, server.Update);
        using var response = await responseTask;
        var responsePacketTypes = UnpackPacketTypes(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([McApiPacketType.AcceptResponse], responsePacketTypes);
    }

    [Fact]
    public async Task Request_WithMalformedAuthorizationHeader_ReturnsUnauthorized()
    {
        using var world = new VoiceCraftWorld();
        using var effects = new AudioEffectSystem();
        using var server = CreateServer(world, effects, out var baseAddress);
        using var client = new HttpClient();
        server.Start();
        using var request = new HttpRequestMessage(HttpMethod.Post, baseAddress)
        {
            Content = new StringContent(Pack(new McApiPingRequestPacket()), Encoding.UTF8, "text/plain")
        };
        request.Headers.TryAddWithoutValidation("Authorization", "x");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ConnectPrefix_DoesNotMatchLongerPath()
    {
        using var world = new VoiceCraftWorld();
        using var effects = new AudioEffectSystem();
        using var server = CreateServer(world, effects, out var baseAddress);
        using var client = new HttpClient();
        server.Start();
        var login = new McApiLoginRequestPacket();
        login.Set("request-1", "login-token", McApiServer.Version, []);

        var responseTask = client.PostAsync(
            new Uri(baseAddress, "connectivity"),
            new StringContent(Pack(login), Encoding.UTF8, "text/plain"));
        await PumpUntilCompletedAsync(responseTask, server.Update);
        using var response = await responseTask;

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, server.ConnectedPeers);
    }

    [Fact]
    public async Task TimedOutPeer_IsRemovedAfterGracePeriod()
    {
        using var world = new VoiceCraftWorld();
        using var effects = new AudioEffectSystem();
        using var server = CreateServer(world, effects, out var baseAddress);
        using var client = new HttpClient();
        server.Config.MaxTimeoutMs = 20;
        server.Start();
        var login = new McApiLoginRequestPacket();
        login.Set("request-1", "login-token", McApiServer.Version, []);
        var responseTask = client.PostAsync(
            new Uri(baseAddress, "connect"),
            new StringContent(Pack(login), Encoding.UTF8, "text/plain"));
        await PumpUntilCompletedAsync(responseTask, server.Update);
        using var response = await responseTask;
        Assert.Single(server.Peers);

        await Task.Delay(60);
        server.Update();

        Assert.Empty(server.Peers);
        Assert.Equal(0, server.ConnectedPeers);
    }

    private static HttpMcApiServer CreateServer(
        VoiceCraftWorld world,
        AudioEffectSystem effects,
        out Uri baseAddress)
    {
        var port = GetFreeTcpPort();
        baseAddress = new Uri($"http://127.0.0.1:{port}/");
        return new HttpMcApiServer(world, effects)
        {
            Config = new HttpMcApiServer.HttpMcApiConfig
            {
                Hostname = baseAddress.ToString(),
                LoginToken = "login-token",
                MaxClients = 2,
                MaxTimeoutMs = 1_000
            }
        };
    }

    private static string Pack(IMcApiPacket packet)
    {
        var packetWriter = new NetDataWriter();
        packetWriter.Put((byte)packet.PacketType);
        packetWriter.Put(packet);

        var payloadWriter = new NetDataWriter();
        payloadWriter.Put((ushort)packetWriter.Length);
        payloadWriter.Put(packetWriter.CopyData());
        return Z85.GetStringWithPadding(payloadWriter.AsReadOnlySpan());
    }

    private static McApiPacketType[] UnpackPacketTypes(string encoded)
    {
        var payload = Z85.GetBytesWithPadding(encoded);
        var reader = new NetDataReader();
        reader.SetSource(payload);
        var packetTypes = new List<McApiPacketType>();
        while (!reader.EndOfData)
        {
            var packetLength = reader.GetUShort();
            var packet = new byte[packetLength];
            reader.GetBytes(packet, packetLength);
            packetTypes.Add((McApiPacketType)packet[0]);
        }

        return packetTypes.ToArray();
    }

    private static async Task PumpUntilCompletedAsync(Task task, Action pump)
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        while (!task.IsCompleted)
        {
            cancellation.Token.ThrowIfCancellationRequested();
            pump();
            await Task.Delay(10, cancellation.Token);
        }
    }

    private static int GetFreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
