using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using LiteNetLib.Utils;
using Spectre.Console;
using VoiceCraft.Core;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.McApiPackets;
using VoiceCraft.Core.Network.McHttpPackets;
using VoiceCraft.Server.Config;
using WatsonWebserver.Core;
using WatsonWebserver.Lite;

namespace VoiceCraft.Server.Servers;

public class McHttpServer
{
    private readonly ConcurrentDictionary<string, McApiNetPeer> _mcApiPeers = [];
    private readonly NetDataReader _reader = new();
    private readonly NetDataWriter _writer = new();

    private WebserverLite? _httpServer;

    //Public Properties
    public McHttpConfig Config { get; private set; } = new();

    public void Start(McHttpConfig? config = null)
    {
        if (config != null)
            Config = config;

        try
        {
            AnsiConsole.WriteLine(Locales.Locales.McHttpServer_Starting);
            var settings = new WebserverSettings();
            _httpServer = new WebserverLite(settings, HandleRequest);
            _httpServer.Start();
            AnsiConsole.MarkupLine($"[green]{Locales.Locales.McHttpServer_Success}[/]");
        }
        catch
        {
            throw new Exception(Locales.Locales.McHttpServer_Exceptions_Failed);
        }
    }

    public void Update()
    {
        foreach (var peer in _mcApiPeers) UpdatePeer(peer.Value);
    }

    public void Stop()
    {
        if (_httpServer == null) return;
        AnsiConsole.WriteLine(Locales.Locales.McHttpServer_Stopping);
        _httpServer.Stop();
        _httpServer.Dispose();
        _httpServer = null;
        AnsiConsole.MarkupLine($"[green]{Locales.Locales.McHttpServer_Stopped}[/]");
    }

    public void SendPacket(McApiNetPeer netPeer, McApiPacket packet)
    {
        _writer.Reset();
        _writer.Put((byte)packet.PacketType);
        packet.Serialize(_writer);
        netPeer.SendPacket(_writer);
    }

    private async Task HandleRequest(HttpContextBase context)
    {
        try
        {
            var packet = await JsonSerializer.DeserializeAsync<McHttpUpdate>(context.Request.Data);
            if (packet == null)
            {
                context.Response.StatusCode = 400;
                await context.Response.Send();
                return;
            }

            var netPeer = GetOrCreatePeer(context.Request.Source.IpAddress);
            var packets = packet.Packets.Split("|");
            foreach (var data in packets.Where(data => data.Length <= short.MaxValue))
                netPeer.ReceiveInboundPacket(Z85.GetBytesWithPadding(data));

            packet.Packets = string.Empty;
            var first = false;
            var stringBuilder = new StringBuilder();
            while (netPeer.RetrieveOutboundPacket(out var outboundPacket))
            {
                stringBuilder.Append(Z85.GetStringWithPadding(outboundPacket));
                if (!first) continue;
                first = true;
                stringBuilder.Append('|');
            }

            packet.Packets = stringBuilder.ToString();
            var responseData = JsonSerializer.Serialize(packet);
            await context.Response.Send(responseData);
        }
        catch (JsonException)
        {
            context.Response.StatusCode = 400;
            await context.Response.Send();
        }
        catch
        {
            context.Response.StatusCode = 500;
            await context.Response.Send();
        }
    }

    private McApiNetPeer GetOrCreatePeer(string ipAddress)
    {
        return _mcApiPeers.GetOrAdd(ipAddress, _ => new McApiNetPeer());
    }

    private void UpdatePeer(McApiNetPeer peer)
    {
        while (peer.RetrieveInboundPacket(out var packetData))
            try
            {
                _reader.Clear();
                _reader.SetSource(packetData);
                var packetType = _reader.GetByte();
                var pt = (McApiPacketType)packetType;
                HandlePacket(pt, _reader, peer);
            }
            catch
            {
                //Do Nothing
            }

        if (peer.Connected && peer.LastPing.Add(TimeSpan.FromSeconds(5)) <= DateTime.UtcNow) peer.Disconnect();
    }

    private void HandlePacket(McApiPacketType packetType, NetDataReader reader, McApiNetPeer peer)
    {
        if (packetType == McApiPacketType.Login && !peer.Connected)
        {
            var loginPacket = new McApiLoginPacket();
            loginPacket.Deserialize(reader);
            HandleLoginPacket(loginPacket, peer);
            return;
        }

        if (!peer.Connected) return;

        // ReSharper disable once UnreachableSwitchCaseDueToIntegerAnalysis
        switch (packetType)
        {
            case McApiPacketType.Logout:
                var logoutPacket = new McApiLogoutPacket();
                logoutPacket.Deserialize(reader);
                HandleLogoutPacket(logoutPacket, peer);
                break;
            case McApiPacketType.Ping:
                var pingPacket = new McApiPingPacket();
                pingPacket.Deserialize(reader);
                HandlePingPacket(pingPacket, peer);
                break;
            case McApiPacketType.Login:
            case McApiPacketType.Accept:
            case McApiPacketType.Deny:
            default:
                break;
        }
    }

    private void HandleLoginPacket(McApiLoginPacket loginPacket, McApiNetPeer netPeer)
    {
        if (!string.IsNullOrEmpty(Config.LoginToken) && Config.LoginToken != loginPacket.LoginToken)
            return;

        netPeer.AcceptConnection(Guid.NewGuid().ToString());
        SendPacket(netPeer, new McApiAcceptPacket(netPeer.SessionToken));
    }

    private static void HandleLogoutPacket(McApiLogoutPacket logoutPacket, McApiNetPeer netPeer)
    {
        if (netPeer.SessionToken != logoutPacket.SessionToken) return;
        netPeer.Disconnect();
    }

    private static void HandlePingPacket(McApiPingPacket pingPacket, McApiNetPeer netPeer)
    {
        if (netPeer.SessionToken != pingPacket.SessionToken) return; //Needs a session token at least.
        netPeer.LastPing = DateTime.UtcNow;
    }
}