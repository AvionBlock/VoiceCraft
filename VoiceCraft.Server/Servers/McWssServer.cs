using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
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
        catch (Exception ex)
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
        SendPacket(socket, Z85.GetStringWithPadding(_writer.CopyData()));
    }

    private void SendPacket(IWebSocketConnection socket, string packetData)
    {
        var packet = new McWssCommandRequest($"{Config.TunnelCommand} \"{packetData}\"");
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

        var first = true;
        var stringBuilder = new StringBuilder();
        while (peer.Value.RetrieveOutboundPacket(out var outboundPacket))
        {
            if(!first)
                stringBuilder.Append('|');
            else
                first = false;
            stringBuilder.Append(Z85.GetStringWithPadding(outboundPacket));
        }

        SendPacket(peer.Key, stringBuilder.ToString());

        switch (peer.Value.Connected)
        {
            case true when DateTime.UtcNow - peer.Value.LastPing >= TimeSpan.FromMilliseconds(Config.MaxTimeoutMs):
                peer.Value.Disconnect();
                break;
        }
    }

    private void OnClientConnected(IWebSocketConnection socket)
    {
        if (_mcApiPeers.Count >= Config.MaxClients)
            socket.Close(); //Full.

        var netPeer = new McApiNetPeer();
        _mcApiPeers.TryAdd(socket, netPeer);
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
                case "commandResponse":
                    var commandResponsePacket = JsonSerializer.Deserialize<McWssCommandResponse>(message);
                    if (commandResponsePacket != null && _mcApiPeers.TryGetValue(socket, out var peer) &&
                        !string.IsNullOrWhiteSpace(commandResponsePacket.StatusMessage) &&
                        commandResponsePacket.StatusCode == 0)
                    {
                        peer.ReceiveInboundPacket(Z85.GetBytesWithPadding(commandResponsePacket.StatusMessage));
                    }

                    break;
            }
        }
        catch
        {
            // ignored
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
            SendPacket(netPeer, new McApiAcceptPacket(packet.RequestId, netPeer.Token));
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