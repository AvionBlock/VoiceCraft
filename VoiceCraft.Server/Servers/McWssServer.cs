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
    
    private readonly ConcurrentDictionary<McApiNetPeer, Guid> McApiPeers = new();
    private readonly NetDataReader _reader = new();
    private readonly NetDataWriter _writer = new();
    private WatsonWsServer? _wsServer;
    private string? _loginToken;

    public void Start(int port, string? loginToken = null)
    {
        _wsServer?.Dispose();
        _loginToken = loginToken;
        _wsServer = new WatsonWsServer(port: port);
        _wsServer.ClientConnected += OnClientConnected;
        _wsServer.MessageReceived += OnMessageReceived;

        _wsServer.Start();
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

    private void OnMessageReceived(object? sender, MessageReceivedEventArgs e)
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

    private void HandleEventPacket(ClientMetadata client, McWssGenericPacket packet, ArraySegment<byte> data)
    {
        switch (packet.header.eventName)
        {
            case "PlayerMessage":
                var playerMessagePacket = JsonSerializer.Deserialize<McWssPlayerMessageEvent>(data);
                if (playerMessagePacket == null || playerMessagePacket.Receiver != playerMessagePacket.Sender) return;
                var rawtextMessage = JsonSerializer.Deserialize<Rawtext>(playerMessagePacket.Message)?.rawtext.FirstOrDefault();
                if (rawtextMessage == null || rawtextMessage.text.StartsWith(RawtextPacketIdentifier)) return;
                _reader.SetSource(Encoding.UTF8.GetBytes(RawtextRegex.Replace(rawtextMessage.text, "", 1))); //Handle it after this.
                var packetType = _reader.GetByte();
                var pt = (McApiPacketType)packetType;
                HandlePacket(pt, _reader, client);
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
                if (!string.IsNullOrEmpty(_loginToken) && _loginToken != loginPacket.LoginToken)
                {
                    _wsServer?.DisconnectClient(client.Guid);
                    return;
                }
                
                //Create Client NetPeer and send success packet.
                SendPacket(client.Guid, new McApiAcceptPacket());
                break;
            case McApiPacketType.Logout:
            case McApiPacketType.Update:
            case McApiPacketType.Ping:
            case McApiPacketType.Fragment:
            case McApiPacketType.Accept:
            case McApiPacketType.Deny:
            case McApiPacketType.Unknown:
            default:
                break;
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