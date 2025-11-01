using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using LiteNetLib.Utils;
using Spectre.Console;
using VoiceCraft.Core;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.McApiPackets;
using VoiceCraft.Core.Network.McWssPackets;
using VoiceCraft.Server.Config;
using Fleck;

namespace VoiceCraft.Server.Servers;

public class McWssServer
{
    private static readonly string SubscribePacket = JsonSerializer.Serialize(new McWssEventSubscribe("PlayerMessage"));
    private static readonly Version McWssVersion = new(1, 1, 0);

    private readonly ConcurrentDictionary<IWebSocketConnection, McApiNetPeer> _mcApiPeers = [];
    private readonly NetDataReader _reader = new();
    private readonly NetDataWriter _writer = new();
    private WebSocketServer? _wsServer;

    //Public Properties
    public McWssConfig Config { get; private set; } = new();

    public void Start(McWssConfig? config = null)
    {
        Stop();

        if (config != null)
            Config = config;

        try
        {
            AnsiConsole.WriteLine(Locales.Locales.McWssServer_Starting);
            _wsServer = new WebSocketServer(Config.Hostname);

            _wsServer.Start(socket =>
            {
                socket.OnOpen = () => OnClientConnected(socket);
                socket.OnClose = () => OnClientDisconnected(socket);
                socket.OnMessage = message => OnMessageReceived(socket, message);
            });
            AnsiConsole.MarkupLine($"[green]{Locales.Locales.McWssServer_Success}[/]");
        }
        catch(Exception ex)
        {
            throw new Exception(Locales.Locales.McWssServer_Exceptions_Failed, ex);
        }
    }

    public void Update()
    {
        foreach (var peer in _mcApiPeers) UpdatePeer(peer);
    }

    public void Stop()
    {
        if (_wsServer == null) return;
        AnsiConsole.WriteLine(Locales.Locales.McWssServer_Stopping);
        foreach (var client in _mcApiPeers)
        {
            try
            {
                client.Key.Close();
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
            }
        }
        _wsServer.Dispose();
        _wsServer = null;
        AnsiConsole.MarkupLine($"[green]{Locales.Locales.McWssServer_Stopped}[/]");
    }

    public void SendPacket(McApiNetPeer netPeer, McApiPacket packet)
    {
        _writer.Reset();
        _writer.Put((byte)packet.PacketType);
        packet.Serialize(_writer);
        netPeer.SendPacket(_writer);
    }

    private void SendPacket(IWebSocketConnection socket, McApiPacket packet)
    {
        _writer.Reset();
        _writer.Put((byte)packet.PacketType);
        packet.Serialize(_writer);
        SendPacket(socket, _writer.CopyData());
    }

    private void SendPacket(IWebSocketConnection socket, byte[] packetData)
    {
        var packet = new McWssCommandRequest($"scriptevent {Config.TunnelId} {Z85.GetStringWithPadding(packetData)}");
        socket.Send(JsonSerializer.Serialize(packet));
    }

    private void UpdatePeer(KeyValuePair<IWebSocketConnection, McApiNetPeer> peer)
    {
        while (peer.Value.RetrieveInboundPacket(out var packetData))
            try
            {
                _reader.Clear();
                _reader.SetSource(packetData);
                var packetType = _reader.GetByte();
                var pt = (McApiPacketType)packetType;
                HandlePacket(pt, _reader, peer.Key, peer.Value);
            }
            catch
            {
                //Do Nothing
            }

        while (peer.Value.RetrieveOutboundPacket(out var outboundPacketData))
            SendPacket(peer.Key, outboundPacketData);

        if (peer.Value.Connected && peer.Value.LastPing.Add(TimeSpan.FromSeconds(5)) <= DateTime.UtcNow)
            peer.Value.Disconnect();
    }

    private void OnClientConnected(IWebSocketConnection socket)
    {
        var netPeer = new McApiNetPeer();
        _mcApiPeers.TryAdd(socket, netPeer);
        socket.Send(SubscribePacket);
    }

    private void OnClientDisconnected(IWebSocketConnection socket)
    {
        if (_mcApiPeers.TryRemove(socket, out var netPeer)) netPeer.Disconnect();
    }

    private void OnMessageReceived(IWebSocketConnection socket, string message)
    {
        try
        {
            var genericPacket = JsonSerializer.Deserialize<McWssGenericPacket>(message);
            if (genericPacket == null) return;

            switch (genericPacket.header.messagePurpose)
            {
                case "event":
                    HandleEventPacket(socket, genericPacket, message);
                    break;
            }
        }
        catch
        {
            // ignored
        }
    }

    private void HandleEventPacket(IWebSocketConnection socket, McWssGenericPacket packet, string message)
    {
        switch (packet.header.eventName)
        {
            case "PlayerMessage":
                var playerMessagePacket = JsonSerializer.Deserialize<McWssPlayerMessageEvent>(message);
                if (playerMessagePacket == null || playerMessagePacket.Receiver != playerMessagePacket.Sender) return;
                var rawtextMessage = JsonSerializer.Deserialize<Rawtext>(playerMessagePacket.Message)?.rawtext
                    .FirstOrDefault();
                if (rawtextMessage == null || !rawtextMessage.text.StartsWith(Config.TunnelId)) return;
                if (_mcApiPeers.TryGetValue(socket, out var peer))
                {
                    var textData = new Regex(Regex.Escape(Config.TunnelId)).Replace(rawtextMessage.text, "", 1);
                    peer.ReceiveInboundPacket(Z85.GetBytesWithPadding(textData));
                }

                break;
        }
    }

    private void HandlePacket(McApiPacketType packetType, NetDataReader reader, IWebSocketConnection socket,
        McApiNetPeer peer)
    {
        if (packetType == McApiPacketType.Login)
        {
            var loginPacket = new McApiLoginPacket();
            loginPacket.Deserialize(reader);
            HandleLoginPacket(loginPacket, socket, peer);
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

    private void HandleLoginPacket(McApiLoginPacket packet, IWebSocketConnection socket, McApiNetPeer netPeer)
    {
        if (netPeer.Connected)
        {
            SendPacket(netPeer, new McApiAcceptPacket(packet.RequestId, packet.Token));
            return;
        }

        if (!string.IsNullOrEmpty(Config.LoginToken) && Config.LoginToken != packet.Token)
        {
            SendPacket(socket,
                new McApiDenyPacket(packet.RequestId, packet.Token, "VcMcApi.DisconnectReason.InvalidLoginToken"));
            return;
        }

        if (packet.Version.Major != McWssVersion.Major || packet.Version.Minor != McWssVersion.Minor)
        {
            SendPacket(socket,
                new McApiDenyPacket(packet.RequestId, packet.Token, "VcMcApi.DisconnectReason.IncompatibleVersion"));
            return;
        }

        netPeer.AcceptConnection(Guid.NewGuid().ToString());
        SendPacket(netPeer, new McApiAcceptPacket(packet.RequestId, netPeer.Token));
    }

    private static void HandleLogoutPacket(McApiLogoutPacket packet, McApiNetPeer netPeer)
    {
        if (netPeer.Token != packet.Token) return;
        netPeer.Disconnect();
    }

    private void HandlePingPacket(McApiPingPacket packet, McApiNetPeer netPeer)
    {
        if (netPeer.Token != packet.Token) return; //Needs a session token at least.
        SendPacket(netPeer, packet); //Reuse the packet.
    }

    //Resharper disable All
    private class Rawtext
    {
        public RawtextMessage[] rawtext { get; set; } = [];
    }

    private class RawtextMessage
    {
        public string text { get; set; } = string.Empty;
    }
    //Resharper enable All
}