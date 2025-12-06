using System.Net;
using System.Net.Sockets;
using System.Numerics;
using VoiceCraft.Core;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Packets.CustomClient;

namespace VoiceCraft.Network.Sockets;

/// <summary>
/// Custom UDP client for handling game client connections with position updates.
/// Implements the VoiceCraft custom client protocol.
/// </summary>
public class CustomClient : Disposable
{
    /// <summary>
    /// IO control code to prevent ICMP connection reset errors on Windows.
    /// </summary>
    public const int SIO_UDP_CONNRESET = -1744830452;

    #region Properties
    /// <summary>
    /// Gets or sets the connection timeout in milliseconds.
    /// </summary>
    public int Timeout { get; set; } = 8000;
    
    /// <summary>
    /// Gets the current socket state.
    /// </summary>
    public CustomClientSocketState State { get; private set; }
    
    /// <summary>
    /// Gets the remote endpoint this client is bound to.
    /// </summary>
    public IPEndPoint RemoteEndpoint { get; private set; } = new(IPAddress.Any, 0);
    
    /// <summary>
    /// Gets or sets the underlying UDP socket.
    /// </summary>
    public Socket Socket { get; private set; } = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

    private PacketRegistry PacketRegistry { get; } = new();
    private CancellationTokenSource CTS { get; set; } = new();
    private SocketAddress? RemoteAddress { get; set; }
    private Task? ActivityChecker { get; set; }
    private long LastActive { get; set; }
    #endregion

    #region Debug Properties
    /// <summary>
    /// Gets or sets whether to log exceptions.
    /// </summary>
    public bool LogExceptions { get; set; }
    
    /// <summary>
    /// Gets or sets whether to log inbound packets.
    /// </summary>
    public bool LogInbound { get; set; }
    
    /// <summary>
    /// Gets or sets whether to log outbound packets.
    /// </summary>
    public bool LogOutbound { get; set; }
    
    /// <summary>
    /// Gets or sets the filter for inbound packet logging.
    /// </summary>
    public List<CustomClientTypes> InboundFilter { get; set; } = [];
    
    /// <summary>
    /// Gets or sets the filter for outbound packet logging.
    /// </summary>
    public List<CustomClientTypes> OutboundFilter { get; set; } = [];
    #endregion

    #region Delegates
    public delegate void StartedHandler();
    public delegate void StoppedHandler(string? reason = null);
    public delegate void ConnectedHandler(string name);
    public delegate void DisconnectedHandler();
    public delegate void UpdatedHandler(Vector3 position, float rotation, float caveDensity, bool isUnderwater, string dimensionId, string levelId, string serverId);
    public delegate void PacketDataHandler<T>(T data, SocketAddress address);
    public delegate void OutboundPacketHandler(CustomClientPacket packet);
    public delegate void InboundPacketHandler(CustomClientPacket packet);
    public delegate void ExceptionErrorHandler(Exception error);
    public delegate void FailedHandler(Exception ex);
    #endregion

    #region Events
    /// <summary>Raised when the socket starts hosting.</summary>
    public event StartedHandler? OnStarted;
    
    /// <summary>Raised when the socket stops.</summary>
    public event StoppedHandler? OnStopped;
    
    /// <summary>Raised when a client connects.</summary>
    public event ConnectedHandler? OnConnected;
    
    /// <summary>Raised when a client disconnects.</summary>
    public event DisconnectedHandler? OnDisconnected;
    
    /// <summary>Raised when position update is received.</summary>
    public event UpdatedHandler? OnUpdated;

    /// <summary>Raised when a login packet is received.</summary>
    public event PacketDataHandler<Login>? OnLoginReceived;
    
    /// <summary>Raised when a logout packet is received.</summary>
    public event PacketDataHandler<Logout>? OnLogoutReceived;
    
    /// <summary>Raised when an accept packet is received.</summary>
    public event PacketDataHandler<Accept>? OnAcceptReceived;
    
    /// <summary>Raised when a deny packet is received.</summary>
    public event PacketDataHandler<Deny>? OnDenyReceived;
    
    /// <summary>Raised when an update packet is received.</summary>
    public event PacketDataHandler<Update>? OnUpdateReceived;

    /// <summary>Raised when a packet is sent (debug).</summary>
    public event OutboundPacketHandler? OnOutboundPacket;
    
    /// <summary>Raised when a packet is received (debug).</summary>
    public event InboundPacketHandler? OnInboundPacket;
    
    /// <summary>Raised when an exception occurs.</summary>
    public event ExceptionErrorHandler? OnExceptionError;
    
    /// <summary>Raised when hosting fails to start.</summary>
    public event FailedHandler? OnFailed;
    #endregion

    /// <summary>
    /// Initializes a new instance of the CustomClient class.
    /// </summary>
    public CustomClient()
    {
        PacketRegistry.RegisterPacket((byte)CustomClientTypes.Login, typeof(Login));
        PacketRegistry.RegisterPacket((byte)CustomClientTypes.Logout, typeof(Logout));
        PacketRegistry.RegisterPacket((byte)CustomClientTypes.Accept, typeof(Accept));
        PacketRegistry.RegisterPacket((byte)CustomClientTypes.Deny, typeof(Deny));
        PacketRegistry.RegisterPacket((byte)CustomClientTypes.Update, typeof(Update));
    }

    #region Methods
    /// <summary>
    /// Starts hosting on the specified port.
    /// </summary>
    /// <param name="port">The port to bind to.</param>
    public async Task HostAsync(int port)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (State != CustomClientSocketState.Stopped)
            throw new InvalidOperationException("You must stop hosting before starting a host!");

        CTS.Dispose();
        State = CustomClientSocketState.Starting;
        CTS = new CancellationTokenSource();

        OnLoginReceived += LoginReceived;
        OnLogoutReceived += LogoutReceived;
        OnUpdateReceived += UpdateReceived;

        try
        {
            RemoteEndpoint = new IPEndPoint(IPAddress.Any, port);
            Socket.Bind(RemoteEndpoint);
            ActivityChecker = Task.Run(CheckerLoopAsync);
            State = CustomClientSocketState.Started;
            OnStarted?.Invoke();
            await ReceiveAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            OnFailed?.Invoke(ex);
            await StopAsync(ex.Message).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Stops hosting and cleans up resources.
    /// </summary>
    /// <param name="reason">Optional reason for stopping.</param>
    public async Task StopAsync(string? reason = null)
    {
        ObjectDisposedException.ThrowIf(IsDisposed && State == CustomClientSocketState.Stopped, this);
        
        if (State is CustomClientSocketState.Stopped or CustomClientSocketState.Stopping)
            return;
            
        State = CustomClientSocketState.Stopping;

        // Unsubscribe from events to prevent memory leaks
        OnLoginReceived -= LoginReceived;
        OnLogoutReceived -= LogoutReceived;
        OnUpdateReceived -= UpdateReceived;

        if (RemoteAddress != null)
        {
            try
            {
                await SocketSendToAsync(new Logout(), RemoteAddress).ConfigureAwait(false);
            }
            catch
            {
                // Ignore send errors during shutdown
            }
        }

        await CTS.CancelAsync().ConfigureAwait(false);
        CTS.Dispose();
        Socket.Close();
        Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        ActivityChecker = null;
        RemoteAddress = null;
        State = CustomClientSocketState.Stopped;
        OnStopped?.Invoke(reason);
    }

    private async Task SocketSendToAsync(CustomClientPacket packet, SocketAddress address)
    {
        List<byte> buffer = [];
        packet.WritePacket(ref buffer);
        await Socket.SendToAsync(buffer.ToArray(), SocketFlags.None, address, CTS.Token).ConfigureAwait(false);

        if (LogOutbound && (OutboundFilter.Count == 0 || OutboundFilter.Contains((CustomClientTypes)packet.PacketId)))
            OnOutboundPacket?.Invoke(packet);
    }

    private async Task ReceiveAsync()
    {
        byte[] buffer = GC.AllocateArray<byte>(length: 65527, pinned: true);
        Memory<byte> bufferMem = buffer.AsMemory();
        var receivedAddress = new SocketAddress(Socket.AddressFamily);

        while (!CTS.IsCancellationRequested)
        {
            try
            {
                _ = await Socket.ReceiveFromAsync(bufferMem, SocketFlags.None, receivedAddress, CTS.Token).ConfigureAwait(false);
                var packet = PacketRegistry.GetCustomPacketFromDataStream(bufferMem.ToArray());

                if (LogInbound && (InboundFilter.Count == 0 || InboundFilter.Contains((CustomClientTypes)packet.PacketId)))
                    OnInboundPacket?.Invoke(packet);

                await HandlePacketReceivedAsync(receivedAddress, packet).ConfigureAwait(false);
            }
            catch (SocketException ex)
            {
                await StopAsync(ex.Message).ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                if (LogExceptions)
                    OnExceptionError?.Invoke(ex);
            }
        }
    }

    private async Task HandlePacketReceivedAsync(SocketAddress address, CustomClientPacket packet)
    {
        switch ((CustomClientTypes)packet.PacketId)
        {
            case CustomClientTypes.Login:
                await HandleLoginAsync((Login)packet, address).ConfigureAwait(false);
                break;
            case CustomClientTypes.Logout:
                await HandleLogoutAsync((Logout)packet, address).ConfigureAwait(false);
                break;
            case CustomClientTypes.Accept:
                OnAcceptReceived?.Invoke((Accept)packet, address);
                break;
            case CustomClientTypes.Deny:
                OnDenyReceived?.Invoke((Deny)packet, address);
                break;
            case CustomClientTypes.Update:
                await HandleUpdateAsync((Update)packet, address).ConfigureAwait(false);
                break;
        }
    }

    private async Task CheckerLoopAsync()
    {
        while (!CTS.IsCancellationRequested)
        {
            var elapsed = Environment.TickCount64 - LastActive;
            if (RemoteAddress != null && elapsed > Timeout)
            {
                RemoteAddress = null;
                OnDisconnected?.Invoke();
                break;
            }

            await Task.Delay(100, CTS.Token).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (State is CustomClientSocketState.Started or CustomClientSocketState.Starting)
                StopAsync().GetAwaiter().GetResult();

            Socket.Dispose();
            CTS.Dispose();
        }
    }
    #endregion

    #region Event Handlers
    private async Task HandleLoginAsync(Login data, SocketAddress address)
    {
        if (RemoteAddress != null && !RemoteAddress.Equals(address))
        {
            await SocketSendToAsync(new Deny { Reason = "Client is already connected to another instance!" }, address).ConfigureAwait(false);
            return;
        }

        LastActive = Environment.TickCount64;
        RemoteAddress = new SocketAddress(address.Family, address.Size);
        await SocketSendToAsync(new Accept(), address).ConfigureAwait(false);
        OnConnected?.Invoke(data.Name);
        OnLoginReceived?.Invoke(data, address);
    }

    // Kept for event compatibility - now wraps async handler
    private async void LoginReceived(Login data, SocketAddress address)
    {
        try
        {
            // Already handled in HandleLoginAsync, this is for external subscribers
        }
        catch (Exception ex)
        {
            if (LogExceptions)
                OnExceptionError?.Invoke(ex);
        }
    }

    private async Task HandleLogoutAsync(Logout data, SocketAddress address)
    {
        if (RemoteAddress != null && RemoteAddress.Equals(address))
        {
            RemoteAddress = null;
            await SocketSendToAsync(new Accept(), address).ConfigureAwait(false);
            OnDisconnected?.Invoke();
            OnLogoutReceived?.Invoke(data, address);
        }
    }

    private async void LogoutReceived(Logout data, SocketAddress address)
    {
        // Handled internally via HandleLogoutAsync
    }

    private async Task HandleUpdateAsync(Update data, SocketAddress address)
    {
        if (RemoteAddress != null && RemoteAddress.Equals(address))
        {
            LastActive = Environment.TickCount64;
            await SocketSendToAsync(new Accept(), address).ConfigureAwait(false);
            OnUpdated?.Invoke(data.Position, data.Rotation, data.CaveDensity, data.IsUnderwater, data.DimensionId, data.LevelId, data.ServerId);
            OnUpdateReceived?.Invoke(data, address);
        }
    }

    private async void UpdateReceived(Update data, SocketAddress address)
    {
        // Handled internally via HandleUpdateAsync
    }
    #endregion
}

/// <summary>
/// Represents the possible states of a CustomClient socket.
/// </summary>
public enum CustomClientSocketState
{
    /// <summary>The socket is stopped.</summary>
    Stopped,
    
    /// <summary>The socket is in the process of stopping.</summary>
    Stopping,
    
    /// <summary>The socket is in the process of starting.</summary>
    Starting,
    
    /// <summary>The socket is started and accepting connections.</summary>
    Started
}

