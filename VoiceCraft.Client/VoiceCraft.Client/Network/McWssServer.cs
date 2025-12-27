using System;
using System.Numerics;
using System.Text.Json;
using Fleck;
using VoiceCraft.Core.Network.McWssPackets;

namespace VoiceCraft.Client.Network;

public class McWssServer(VoiceCraftClient client) : IDisposable
{
    public bool IsStarted { get; private set; }
    private WebSocketServer? _wsServer;
    private IWebSocketConnection? _peerConnection;
    private string _localPlayerRequestId = string.Empty;
    private string _localPlayerName = string.Empty;
    private VoiceCraftClient _client = client;

    public event Action<string>? OnConnected;
    public event Action? OnDisconnected;

    public void Start(string ip, int port)
    {
        Stop();

        try
        {
            _wsServer = new WebSocketServer($"ws://{ip}:{port}");

            _wsServer.Start(socket =>
            {
                socket.OnOpen = () => OnClientConnected(socket);
                socket.OnClose = () => OnClientDisconnected(socket);
                socket.OnMessage = message => OnMessageReceived(socket, message);
            });
            IsStarted = true;
        }
        catch (Exception ex)
        {
            throw new Exception("McWssServer.Exceptions.Failed", ex);
        }
    }

    public void Stop()
    {
        try
        {
            if (_wsServer == null) return;
            _peerConnection?.Close();
            _wsServer.Dispose();
            _wsServer = null;
        }
        finally
        {
            IsStarted = false;
        }
    }

    public void Dispose()
    {
        Stop();
        OnConnected = null;
        OnDisconnected = null;
        GC.SuppressFinalize(this);
    }

    private static string SendCommand(IWebSocketConnection socket, string command)
    {
        var packet = new McWssCommandRequest(command);
        socket.Send(JsonSerializer.Serialize(packet));
        return packet.header.requestId;
    }

    private void SendEventSubscribe(IWebSocketConnection socket, string eventName)
    {
        var packet = new McWssEventSubscribeRequest(eventName);
        socket.Send(JsonSerializer.Serialize(packet));
    }

    private void OnClientConnected(IWebSocketConnection socket)
    {
        if (_peerConnection != null)
            socket.Close(); //Full.

        _peerConnection = socket;
        SendEventSubscribe(_peerConnection, "PlayerTravelled");
        SendEventSubscribe(_peerConnection, "PlayerTransform");
        SendEventSubscribe(_peerConnection, "PlayerTeleported");
        _localPlayerRequestId = SendCommand(_peerConnection, "/getlocalplayername");
    }

    private void OnClientDisconnected(IWebSocketConnection socket)
    {
        if (_peerConnection?.ConnectionInfo.Id != socket.ConnectionInfo.Id) return;
        _peerConnection = null;
        OnDisconnected?.Invoke();
    }

    private void OnMessageReceived(IWebSocketConnection _, string message)
    {
        try
        {
            var genericPacket = JsonSerializer.Deserialize<McWssGenericPacket>(message);
            if (genericPacket == null) return;

            switch (genericPacket.header.messagePurpose)
            {
                case "commandResponse":
                    HandleCommandResponse(genericPacket, message);
                    break;
                case "event":
                    HandleEvent(genericPacket, message);
                    break;
            }
        }
        catch
        {
            //Do Nothing
        }
    }

    private void HandleCommandResponse(McWssGenericPacket genericPacket, string data)
    {
        if (genericPacket.header.requestId != _localPlayerRequestId) return;
        var localPlayerNameCommandResponse = JsonSerializer.Deserialize<McWssLocalPlayerNameCommandResponse>(data);
        if(localPlayerNameCommandResponse == null) return;
        _localPlayerName = localPlayerNameCommandResponse.LocalPlayerName;
        _client.Name = _localPlayerName;
        OnConnected?.Invoke(_localPlayerName);
    }

    private void HandleEvent(McWssGenericPacket genericPacket, string data)
    {
        switch (genericPacket.header.eventName)
        {
            case "PlayerTravelled":
                var playerTravelledEventPacket = JsonSerializer.Deserialize<McWssPlayerTravelledEvent>(data);
                if(playerTravelledEventPacket == null) return;
                HandlePlayerTravelledEvent(playerTravelledEventPacket);
                break;
            case "PlayerTransform":
                var playerTransformEventPacket = JsonSerializer.Deserialize<McWssPlayerTransformEvent>(data);
                if(playerTransformEventPacket == null) return;
                HandlePlayerTransformEvent(playerTransformEventPacket);
                break;
            case "PlayerTeleported":
                var playerTeleportedEvent = JsonSerializer.Deserialize<McWssPlayerTeleportedEvent>(data);
                if(playerTeleportedEvent == null) return;
                HandlePlayerTeleportedEvent(playerTeleportedEvent);
                break;
        }
    }

    private void HandlePlayerTravelledEvent(McWssPlayerTravelledEvent playerTravelledEvent)
    {
        if (playerTravelledEvent.body.player.name != _localPlayerName) return;
        var position = playerTravelledEvent.body.player.position;
        var rotation = playerTravelledEvent.body.player.yRot;
        var dimensionId = playerTravelledEvent.body.player.dimension;
        _client.Position = new Vector3(position.x, position.y, position.z);
        _client.Rotation = new Vector2(0, rotation);
        _client.WorldId = dimensionId switch
        {
            0 => "minecraft:overworld",
            1 => "minecraft:nether",
            2 => "minecraft:end",
            _ => $"{dimensionId}"
        };
    }
    
    private void HandlePlayerTransformEvent(McWssPlayerTransformEvent playerTransformEvent)
    {
        if (playerTransformEvent.body.player.name != _localPlayerName) return;
        var position = playerTransformEvent.body.player.position;
        var rotation = playerTransformEvent.body.player.yRot;
        var dimensionId = playerTransformEvent.body.player.dimension;
        _client.Position = new Vector3(position.x, position.y, position.z);
        _client.Rotation = new Vector2(0, rotation);
        _client.WorldId = dimensionId switch
        {
            0 => "minecraft:overworld",
            1 => "minecraft:nether",
            2 => "minecraft:end",
            _ => $"{dimensionId}"
        };
    }

    private void HandlePlayerTeleportedEvent(McWssPlayerTeleportedEvent playerTeleportedEvent)
    {
        if (playerTeleportedEvent.body.player.name != _localPlayerName) return;
        var position = playerTeleportedEvent.body.player.position;
        var rotation = playerTeleportedEvent.body.player.yRot;
        var dimensionId = playerTeleportedEvent.body.player.dimension;
        _client.Position = new Vector3(position.x, position.y, position.z);
        _client.Rotation = new Vector2(0, rotation);
        _client.WorldId = dimensionId switch
        {
            0 => "minecraft:overworld",
            1 => "minecraft:nether",
            2 => "minecraft:end",
            _ => $"{dimensionId}"
        };
    }
}