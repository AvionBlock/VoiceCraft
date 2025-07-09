using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LiteNetLib.Utils;
using Spectre.Console;
using VoiceCraft.Core;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.McApiPackets;
using VoiceCraft.Core.Network.McWssPackets;
using VoiceCraft.Server.Config;
using WatsonWebsocket;

namespace VoiceCraft.Server.Servers;

public class McWssServer
{
    private static readonly string SubscribePacket = JsonSerializer.Serialize(new McWssEventSubscribe("PlayerMessage"));
    private static readonly Regex RawtextRegex = new(Regex.Escape(Constants.RawtextPacketIdentifier));

    //Public Properties
    public McWssConfig Config { get; private set; } = new();

    private readonly ConcurrentDictionary<ClientMetadata, McApiNetPeer> _mcApiPeers = [];
    private readonly NetDataReader _reader = new();
    private readonly NetDataWriter _writer = new();
    private WatsonWsServer? _wsServer;

    public void Start(McWssConfig? config = null)
    {
        Stop();

        if (config != null)
            Config = config;

        try
        {
            AnsiConsole.WriteLine(Locales.Locales.McWssServer_Starting);
            _wsServer = new WatsonWsServer(port: (int)Config.Port);

            _wsServer.ClientConnected += OnClientConnected;
            _wsServer.ClientDisconnected += OnClientDisconnected;
            _wsServer.MessageReceived += OnMessageReceived;

            _wsServer.Start();
            AnsiConsole.MarkupLine($"[green]{Locales.Locales.McWssServer_Success}[/]");
        }
        catch
        {
            if (_wsServer == null) throw new Exception(Locales.Locales.McWssServer_Exceptions_Failed);
            _wsServer.ClientConnected -= OnClientConnected;
            _wsServer.ClientDisconnected += OnClientDisconnected;
            _wsServer.MessageReceived += OnMessageReceived;
            throw new Exception(Locales.Locales.McWssServer_Exceptions_Failed);
        }
    }

    public void Update()
    {
        foreach (var peer in _mcApiPeers)
        {
            while (peer.Value.RetrieveInboundPacket(out var packetData))
            {
                _reader.Clear();
                _reader.SetSource(packetData);
                var packetType = _reader.GetByte();
                var pt = (McApiPacketType)packetType;
                HandlePacket(pt, _reader, peer.Key, peer.Value);
            }

            while (peer.Value.RetrieveOutboundPacket(out var outboundPacketData))
            {
                SendPacket(peer.Key.Guid, outboundPacketData);
            }

            if (peer.Value.LastPing.Add(TimeSpan.FromSeconds(5)) <= DateTime.UtcNow)
            {
                _wsServer?.DisconnectClient(peer.Key.Guid);
            }
        }
    }

    public void Stop()
    {
        if (_wsServer == null) return;
        AnsiConsole.WriteLine(Locales.Locales.McWssServer_Stopping);
        _wsServer.ClientConnected -= OnClientConnected;
        _wsServer.ClientDisconnected -= OnClientDisconnected;
        _wsServer.MessageReceived -= OnMessageReceived;
        _wsServer.Dispose();
        _wsServer = null;
        AnsiConsole.WriteLine(Locales.Locales.McWssServer_Stopped);
    }

    public void SendPacket(McApiNetPeer netPeer, McApiPacket packet)
    {
        _writer.Reset();
        _writer.Put((byte)packet.PacketType);
        packet.Serialize(_writer);
        netPeer.SendPacket(_writer);
    }

    private void SendPacket(Guid clientGuid, byte[] packetData)
    {
        var packet = new McWssCommandRequest($"scripevent vc:mcapi {Encoding.UTF8.GetString(packetData, 0, packetData.Length)}");
        _wsServer?.SendAsync(clientGuid, JsonSerializer.Serialize(packet));
    }

    private void OnClientConnected(object? sender, ConnectionEventArgs e)
    {
        var netPeer = new McApiNetPeer();
        _mcApiPeers.TryAdd(e.Client, netPeer);
        e.Client.Metadata = netPeer;
        _wsServer?.SendAsync(e.Client.Guid, SubscribePacket);
    }

    private void OnClientDisconnected(object? sender, DisconnectionEventArgs e)
    {
        _mcApiPeers.TryRemove(e.Client, out _);
    }

    private void OnMessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        try
        {
            if (e.MessageType != WebSocketMessageType.Text)
                return;

            var genericPacket = JsonSerializer.Deserialize<McWssGenericPacket>(e.Data);
            if (genericPacket == null) return;

            switch (genericPacket.header.messagePurpose)
            {
                case "event":
                    HandleEventPacket(e.Client, genericPacket, e.Data);
                    break;
            }
        }
        catch
        {
            // ignored
        }
    }

    private void HandleEventPacket(ClientMetadata client, McWssGenericPacket packet, ArraySegment<byte> data)
    {
        switch (packet.header.eventName)
        {
            case "PlayerMessage":
                var playerMessagePacket = JsonSerializer.Deserialize<McWssPlayerMessageEvent>(data);
                if (playerMessagePacket == null || playerMessagePacket.Receiver != playerMessagePacket.Sender) return;
                var rawtextMessage = JsonSerializer.Deserialize<Rawtext>(playerMessagePacket.Message)?.rawtext.FirstOrDefault();
                if (rawtextMessage == null || rawtextMessage.text.StartsWith(Constants.RawtextPacketIdentifier)) return;
                if (_mcApiPeers.TryGetValue(client, out var peer))
                    peer.ReceiveInboundPacket(Encoding.UTF8.GetBytes(RawtextRegex.Replace(rawtextMessage.text, "", 1)));
                break;
        }
    }

    private void HandlePacket(McApiPacketType packetType, NetDataReader reader, ClientMetadata client, McApiNetPeer peer)
    {
        if (packetType == McApiPacketType.Login && !peer.Connected)
        {
            var loginPacket = new McApiLoginPacket();
            loginPacket.Deserialize(reader);
            HandleLoginPacket(loginPacket, client);
            return;
        }
        if (!peer.Connected) return;
        
        // ReSharper disable once UnreachableSwitchCaseDueToIntegerAnalysis
        switch (packetType)
        {
            case McApiPacketType.Logout:
                var logoutPacket = new McApiLogoutPacket();
                logoutPacket.Deserialize(reader);
                HandleLogoutPacket(logoutPacket, client);
                break;
            case McApiPacketType.Ping:
                var pingPacket = new McApiPingPacket();
                pingPacket.Deserialize(reader);
                HandlePingPacket(pingPacket, client);
                break;
            case McApiPacketType.Login:
            case McApiPacketType.Accept:
            case McApiPacketType.Deny:
            case McApiPacketType.Unknown:
            default:
                break;
        }
    }

    private void HandleLoginPacket(McApiLoginPacket loginPacket, ClientMetadata client)
    {
        if (!string.IsNullOrEmpty(Config.LoginToken) && Config.LoginToken != loginPacket.LoginToken)
        {
            _wsServer?.DisconnectClient(client.Guid);
            return;
        }

        if (!_mcApiPeers.TryGetValue(client, out var netPeer)) return;
        netPeer.AcceptConnection(Guid.NewGuid().ToString());
        SendPacket(netPeer, new McApiAcceptPacket(netPeer.SessionToken));
    }

    private void HandleLogoutPacket(McApiLogoutPacket logoutPacket, ClientMetadata client)
    {
        if (string.IsNullOrWhiteSpace(logoutPacket.SessionToken)) return; //Needs a session token at least.
        if (_mcApiPeers.TryGetValue(client, out var netPeer) && netPeer.SessionToken == logoutPacket.SessionToken)
            _wsServer?.DisconnectClient(client.Guid);
    }

    private void HandlePingPacket(McApiPingPacket pingPacket, ClientMetadata client)
    {
        if (string.IsNullOrWhiteSpace(pingPacket.SessionToken)) return; //Needs a session token at least.
        if (_mcApiPeers.TryGetValue(client, out var netPeer) && netPeer.SessionToken == pingPacket.SessionToken)
        {
            netPeer.LastPing = DateTime.UtcNow;
        }
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