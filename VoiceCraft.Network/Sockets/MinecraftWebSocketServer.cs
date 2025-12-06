using Fleck;
using System.Diagnostics;
using System.Numerics;
using VoiceCraft.Core;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Packets.MCWSS;

namespace VoiceCraft.Network.Sockets;

/// <summary>
/// Minecraft WebSocket Server for receiving player position updates from Bedrock Edition.
/// Uses the Fleck WebSocket library to handle connections.
/// </summary>
public class MinecraftWebSocketServer : Disposable
{
#pragma warning disable CA1031 // Do not catch general exception types
    private WebSocketServer _socket;
    private IWebSocketConnection? _connectedSocket;
    private readonly string[] _dimensions;
    private readonly int _port;
    private readonly MCPacketRegistry _packetRegistry;
    private readonly object _connectionLock = new();

    /// <summary>
    /// Gets whether a client is currently connected.
    /// </summary>
    public bool IsConnected { get; private set; }

    #region Events
    /// <summary>Raised when a player connects.</summary>
    public event EventHandler<MCWSSConnectedEventArgs>? OnConnected;
    
    /// <summary>Raised when hosting fails.</summary>
    public event EventHandler<MCWSSErrorEventArgs>? OnFailed;
    
    /// <summary>Raised when a player moves.</summary>
    public event EventHandler<PlayerTravelledEventArgs>? OnPlayerTravelled;
    
    /// <summary>Raised when a player disconnects.</summary>
    public event EventHandler? OnDisconnected;
    #endregion



    /// <summary>
    /// Initializes a new instance of the MinecraftWebSocketServer class.
    /// </summary>
    /// <param name="port">The port to listen on.</param>
    public MinecraftWebSocketServer(int port)
    {
        _port = port;
        _packetRegistry = new MCPacketRegistry();
        _packetRegistry.RegisterPacket(
            new Header { messagePurpose = "event", eventName = nameof(PlayerTravelled) }, 
            typeof(MCWSSPacket<PlayerTravelled>));
        _packetRegistry.RegisterPacket(
            new Header { messagePurpose = "commandResponse" }, 
            typeof(MCWSSPacket<LocalPlayerName>));
        
        _socket = new WebSocketServer($"ws://0.0.0.0:{port}");
        _dimensions = ["minecraft:overworld", "minecraft:nether", "minecraft:end"];
    }

    /// <summary>
    /// Starts the WebSocket server.
    /// </summary>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        
        try
        {
            _socket.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    lock (_connectionLock)
                    {
                        if (_connectedSocket == null)
                        {
                            // https://gist.github.com/jocopa3/5f718f4198f1ea91a37e3a9da468675c
                            socket.Send(new MCWSSPacket<Command>
                            {
                                header = { messagePurpose = "commandRequest", requestId = Guid.NewGuid().ToString() },
                                body = { commandLine = "/getlocalplayername" }
                            }.SerializePacket());
                            
                            socket.Send(new MCWSSPacket<MCWSSEvent>
                            {
                                header = { requestId = Guid.NewGuid().ToString(), messagePurpose = "subscribe" },
                                body = { eventName = "PlayerTravelled" }
                            }.SerializePacket());
                            
                            _connectedSocket = socket;
                            IsConnected = true;
                        }
                        else
                        {
                            socket.Close();
                        }
                    }
                };

                socket.OnClose = () =>
                {
                    lock (_connectionLock)
                    {
                        if (socket == _connectedSocket)
                        {
                            _connectedSocket = null;
                            IsConnected = false;
                            OnDisconnected?.Invoke(this, EventArgs.Empty);
                        }
                    }
                };

                socket.OnMessage = message =>
                {
                    try
                    {
                        var packet = _packetRegistry.GetPacketFromJsonString(message);

                        if (packet is MCWSSPacket<LocalPlayerName> localPlayerPacket)
                        {
                            var name = localPlayerPacket.body.localplayername;
                            OnConnected?.Invoke(this, new MCWSSConnectedEventArgs(name));
                        }
                        else if (packet is MCWSSPacket<PlayerTravelled> travelledPacket)
                        {
                            var position = new Vector3(
                                travelledPacket.body.player.position.x,
                                travelledPacket.body.player.position.y,
                                travelledPacket.body.player.position.z);
                            
                            var dimensionIndex = travelledPacket.body.player.dimension;
                            var dimension = dimensionIndex >= 0 && dimensionIndex < _dimensions.Length
                                ? _dimensions[dimensionIndex]
                                : "minecraft:overworld";

                            OnPlayerTravelled?.Invoke(this, new PlayerTravelledEventArgs(position, dimension));
#if DEBUG
                            Debug.WriteLine($"PlayerTravelled: {position.X}, {position.Y}, {position.Z}, {dimension}");
#endif
                        }
                    }
                    catch (Exception)
                    {
                        // Silently ignore malformed packets
                    }
                };
            });
        }
        catch (Exception ex)
        {
            OnFailed?.Invoke(this, new MCWSSErrorEventArgs(ex));
        }
    }

    /// <summary>
    /// Stops the WebSocket server.
    /// </summary>
    public void Stop()
    {
        lock (_connectionLock)
        {
            _connectedSocket?.Close();
            _connectedSocket = null;
        }
        
        _socket.Dispose();
        _socket = new WebSocketServer($"ws://0.0.0.0:{_port}");
        IsConnected = false;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Stop();
            _socket.Dispose();
        }
        base.Dispose(disposing);
    }
}

public class PlayerTravelledEventArgs : EventArgs
{
    public Vector3 Position { get; }
    public string Dimension { get; }

    public PlayerTravelledEventArgs(Vector3 position, string dimension)
    {
        Position = position;
        Dimension = dimension;
    }
}

public class MCWSSConnectedEventArgs : EventArgs
{
    public string Name { get; }
    public MCWSSConnectedEventArgs(string name) => Name = name;
}

public class MCWSSErrorEventArgs : EventArgs
{
    public Exception Error { get; }
    public MCWSSErrorEventArgs(Exception error) => Error = error;
}
