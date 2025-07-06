using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.McApiPackets;
using VoiceCraft.Core.Network.McWssPackets;
using WatsonWebsocket;

namespace VoiceCraft.Server.Servers;

public class McWssServer
{
    private static readonly string SubscribePacket = JsonSerializer.Serialize(new McWssEventSubscribe("PlayerMessage"));
    private static readonly Regex RawtextRegex = new(Regex.Escape(RawtextPacketIdentifier));
    private const string RawtextPacketIdentifier = "§p§k";
    
    private readonly ConcurrentDictionary<ClientMetadata, McApiNetPeer> _mcApiPeers = [];
    private readonly NetDataReader _internalReader = new();
    private readonly NetDataReader _reader = new();
    private readonly NetDataWriter _writer = new();
    private WatsonWsServer? _wsServer;
    private string? _loginToken;

    public void Start(int port, string? loginToken = null)
    {
        Stop();
        _loginToken = loginToken;
        _wsServer = new WatsonWsServer(port: port);
        _wsServer.ClientConnected += OnClientConnected;
        _wsServer.ClientDisconnected += OnClientDisconnected;
        _wsServer.MessageReceived += OnMessageReceived;

        _wsServer.Start();
    }

    public void Update()
    {
        foreach (var peer in _mcApiPeers)
        {
            while (peer.Value.PacketQueue.TryDequeue(out var packetData))
            {
                _reader.Clear();
                _reader.SetSource(packetData);
                var packetType = _reader.GetByte();
                var pt = (McApiPacketType)packetType;
                HandlePacket(pt, _reader, peer.Key);
            }
        }
    }

    public void Stop()
    {
        _wsServer?.Dispose();
        _wsServer = null;
    }

    private void SendPacket(Guid clientGuid, McApiPacket packet)
    {
        _writer.Reset();
        packet.Serialize(_writer);
        _wsServer?.SendAsync(clientGuid, Encoding.UTF8.GetString(_writer.Data, 0, _writer.Length));
        //Will need to convert to scriptevent command request.
    }

    private void OnClientConnected(object? sender, ConnectionEventArgs e)
    {
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
            {
                _wsServer?.DisconnectClient(e.Client.Guid);
                return;
            }

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
            _wsServer?.DisconnectClient(e.Client.Guid);
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
                if (rawtextMessage == null || rawtextMessage.text.StartsWith(RawtextPacketIdentifier)) return;
                _internalReader.Clear();
                _internalReader.SetSource(Encoding.UTF8.GetBytes(RawtextRegex.Replace(rawtextMessage.text, "", 1))); //Handle it after this.
                var packetType = _internalReader.GetByte();
                var pt = (McApiPacketType)packetType;
                HandlePacket(pt, _internalReader, client);
                break;
        }
    }

    private void HandlePacket(McApiPacketType packetType, NetDataReader reader, ClientMetadata client)
    {
        switch (packetType)
        {
            case McApiPacketType.Login:
                var loginPacket = new McApiLoginPacket();
                loginPacket.Deserialize(reader);
                HandleLoginPacket(loginPacket, client);
                break;
            case McApiPacketType.Logout:
                var logoutPacket = new McApiLogoutPacket();
                logoutPacket.Deserialize(reader);
                HandleLogoutPacket(logoutPacket, client);
                break;
            case McApiPacketType.Update:
            case McApiPacketType.Ping:
            case McApiPacketType.Fragment:
            case McApiPacketType.Accept:
            case McApiPacketType.Deny:
            case McApiPacketType.Unknown:
            default:
                if (client.Metadata is not McApiNetPeer netPeer) return;
                netPeer.PacketQueue.Enqueue(reader.GetRemainingBytes());
                break;
        }
    }

    private void HandleLoginPacket(McApiLoginPacket loginPacket, ClientMetadata client)
    {
        if (!string.IsNullOrEmpty(_loginToken) && _loginToken != loginPacket.LoginToken)
        {
            _wsServer?.DisconnectClient(client.Guid);
            return;
        }

        var netPeer = new McApiNetPeer();
        client.Metadata = netPeer;
        _mcApiPeers.TryAdd(client, netPeer);
        SendPacket(client.Guid, new McApiAcceptPacket());
    }

    private void HandleLogoutPacket(McApiLogoutPacket logoutPacket, ClientMetadata client)
    {
        if (string.IsNullOrWhiteSpace(logoutPacket.SessionToken)) return; //Needs a session token at least.
        _wsServer?.DisconnectClient(client.Guid);
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