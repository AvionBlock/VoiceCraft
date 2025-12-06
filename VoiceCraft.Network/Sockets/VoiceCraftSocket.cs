using System.Net.Sockets;
using System.Net;
using VoiceCraft.Core;
using VoiceCraft.Core.Packets;
using System.Collections.Concurrent;
using VoiceCraft.Core.Packets.VoiceCraft;

namespace VoiceCraft.Network.Sockets;

using System.Collections.ObjectModel;

/// <summary>
/// Main VoiceCraft UDP socket handler for voice communication.
/// Supports both client and server modes with packet reliability and rate limiting.
/// </summary>
public class VoiceCraftSocket : Disposable
{
#pragma warning disable CA1031 // Do not catch general exception types
    /// <summary>
    /// Maximum time in milliseconds between send operations.
    /// </summary>
    public const long MaxSendTime = 100;

    /// <summary>
    /// Windows socket control code to ignore UDP connection reset errors.
    /// </summary>
    public const int SioUdpConnReset = -1744830452;

    #region Variables
    /// <summary>
    /// Gets or sets the packet registry for serialization/deserialization.
    /// </summary>
    public PacketRegistry PacketRegistry { get; set; } = new();

    /// <summary>
    /// Gets the underlying UDP socket.
    /// </summary>
    public Socket Socket { get; private set; } = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

    /// <summary>
    /// Gets the remote endpoint for client connections.
    /// </summary>
    public IPEndPoint RemoteEndpoint { get; private set; } = new(IPAddress.Any, 0);

    /// <summary>
    /// Gets the current socket state.
    /// </summary>
    public VoiceCraftSocketState State { get; private set; }

    /// <summary>
    /// Gets or sets the connection timeout in milliseconds.
    /// </summary>
    public int Timeout { get; set; } = 8000;

    /// <summary>
    /// Gets whether the client is connected.
    /// </summary>
    public bool IsConnected { get; private set; }

    // Private Variables
    private CancellationTokenSource CTS { get; set; } = new();
    private ConcurrentDictionary<EndPoint, NetPeer> NetPeers { get; set; } = new();
    private ConcurrentDictionary<string, RateLimitInfo> _rateLimits = new();
    private const int PacketsPerSecondLimit = 10;
    private NetPeer? ClientNetPeer { get; set; }
    private Task? ActivityChecker { get; set; }
    private Task? Sender { get; set; }
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceCraftSocket"/> class and registers all packet types.
    /// </summary>
    public VoiceCraftSocket()
    {
        PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Login, typeof(Login));
        PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Logout, typeof(Logout));
        PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Accept, typeof(Accept));
        PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Deny, typeof(Deny));
        PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Ack, typeof(Ack));
        PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Ping, typeof(Ping));
        PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.PingInfo, typeof(PingInfo));
        PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Binded, typeof(Binded));
        PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Unbinded, typeof(Unbinded));
        PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.ParticipantJoined, typeof(ParticipantJoined));
        PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.ParticipantLeft, typeof(ParticipantLeft));
        PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Mute, typeof(Mute));
        PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Unmute, typeof(Unmute));
        PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Deafen, typeof(Deafen));
        PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Undeafen, typeof(Undeafen));
        PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.JoinChannel, typeof(JoinChannel));
        PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.LeaveChannel, typeof(LeaveChannel));
        PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.AddChannel, typeof(AddChannel));
        PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.RemoveChannel, typeof(RemoveChannel));
        PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.UpdatePosition, typeof(UpdatePosition));
        PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.FullUpdatePosition, typeof(FullUpdatePosition));
        PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.UpdateEnvironmentId, typeof(UpdateEnvironmentId));
        PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.ClientAudio, typeof(ClientAudio));
        PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.ServerAudio, typeof(ServerAudio));
    }

    #region Debug Settings
    /// <summary>Gets or sets whether to log exceptions.</summary>
    public bool LogExceptions { get; set; }
    /// <summary>Gets or sets whether to log inbound packets.</summary>
    public bool LogInbound { get; set; }
    /// <summary>Gets or sets whether to log outbound packets.</summary>
    public bool LogOutbound { get; set; }
    /// <summary>Gets or sets the inbound packet type filter.</summary>
    public Collection<VoiceCraftPacketTypes> InboundFilter { get; } = [];
    /// <summary>Gets or sets the outbound packet type filter.</summary>
    public Collection<VoiceCraftPacketTypes> OutboundFilter { get; } = [];
    #endregion

    #region Delegates
    /// <summary>Delegate for client connected event.</summary>
    /// <summary>Delegate for client connected event.</summary>
    public delegate void Connected(short key);
    /// <summary>Delegate for client disconnected event.</summary>
    public delegate void Disconnected(string? reason = null);

    /// <summary>Delegate for server started event.</summary>
    public delegate void Started();
    /// <summary>Delegate for server stopped event.</summary>
    public delegate void Stopped(string? reason = null);
    /// <summary>Delegate for peer connected event.</summary>
    public delegate void PeerConnected(NetPeer peer, Login packet);
    /// <summary>Delegate for peer disconnected event.</summary>
    public delegate void PeerDisconnected(NetPeer peer, string? reason = null);

    #endregion

    #region Events
    /// <summary>Occurs when client connects successfully.</summary>
    public event EventHandler<VoiceCraftConnectedEventArgs>? OnConnected;
    /// <summary>Occurs when client disconnects.</summary>
    public event EventHandler<VoiceCraftDisconnectedEventArgs>? OnDisconnected;

    /// <summary>Occurs when server starts.</summary>
    public event EventHandler? OnStarted;
    /// <summary>Occurs when server stops.</summary>
    public event EventHandler<VoiceCraftStoppedEventArgs>? OnStopped;
    /// <summary>Occurs when a peer connects to server.</summary>
    public event EventHandler<PacketEventArgs<Login>>? OnPeerConnected;
    /// <summary>Occurs when a peer disconnects from server.</summary>
    public event EventHandler<PacketEventArgs<string?>>? OnPeerDisconnected;

    /// <summary>Occurs when a Login packet is received.</summary>
    public event EventHandler<PacketEventArgs<Login>>? OnLoginReceived;
    public event EventHandler<PacketEventArgs<Logout>>? OnLogoutReceived;
    public event EventHandler<PacketEventArgs<Accept>>? OnAcceptReceived;
    public event EventHandler<PacketEventArgs<Deny>>? OnDenyReceived;
    public event EventHandler<PacketEventArgs<Ack>>? OnAckReceived;
    public event EventHandler<PacketEventArgs<Ping>>? OnPingReceived;
    public event EventHandler<PacketEventArgs<PingInfo>>? OnPingInfoReceived;
    public event EventHandler<PacketEventArgs<Binded>>? OnBindedReceived;
    public event EventHandler<PacketEventArgs<Unbinded>>? OnUnbindedReceived;
    public event EventHandler<PacketEventArgs<ParticipantJoined>>? OnParticipantJoinedReceived;
    public event EventHandler<PacketEventArgs<ParticipantLeft>>? OnParticipantLeftReceived;
    public event EventHandler<PacketEventArgs<Mute>>? OnMuteReceived;
    public event EventHandler<PacketEventArgs<Unmute>>? OnUnmuteReceived;
    public event EventHandler<PacketEventArgs<Deafen>>? OnDeafenReceived;
    public event EventHandler<PacketEventArgs<Undeafen>>? OnUndeafenReceived;
    public event EventHandler<PacketEventArgs<JoinChannel>>? OnJoinChannelReceived;
    public event EventHandler<PacketEventArgs<LeaveChannel>>? OnLeaveChannelReceived;
    public event EventHandler<PacketEventArgs<AddChannel>>? OnAddChannelReceived;
    public event EventHandler<PacketEventArgs<RemoveChannel>>? OnRemoveChannelReceived;
    public event EventHandler<PacketEventArgs<UpdatePosition>>? OnUpdatePositionReceived;
    public event EventHandler<PacketEventArgs<FullUpdatePosition>>? OnFullUpdatePositionReceived;
    public event EventHandler<PacketEventArgs<UpdateEnvironmentId>>? OnUpdateEnvironmentIdReceived;
    public event EventHandler<PacketEventArgs<ClientAudio>>? OnClientAudioReceived;
    public event EventHandler<PacketEventArgs<ServerAudio>>? OnServerAudioReceived;

    //Error and Debug Events
    public event EventHandler<PacketEventArgs<VoiceCraftPacket>>? OnOutboundPacket;
    public event EventHandler<PacketEventArgs<VoiceCraftPacket>>? OnInboundPacket;
    public event EventHandler<VoiceCraftErrorEventArgs>? OnExceptionError;
    public event EventHandler<VoiceCraftErrorEventArgs>? OnFailed;
    #endregion



        #region Methods
        public async Task ConnectAsync(string IP, int port, short preferredKey, PositioningTypes positioningType, string version)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, nameof(VoiceCraftSocket));
            if (State == VoiceCraftSocketState.Started || State == VoiceCraftSocketState.Starting) throw new InvalidOperationException("Cannot start connection as socket is in a hosting state!");
            if (State != VoiceCraftSocketState.Stopped) throw new InvalidOperationException("You must disconnect before reconnecting!");

            CTS.Dispose(); //Prevent memory leak for startup.
            //Socket.IOControl((IOControlCode)SioUdpConnReset, [0, 0, 0, 0], null); //I fucking hate this Windows Only

            //Reset/Setup
            State = VoiceCraftSocketState.Connecting;
            CTS = new CancellationTokenSource();
            ClientNetPeer = new NetPeer(RemoteEndpoint, long.MinValue);
            ClientNetPeer.OnPacketReceived += HandlePacketReceived;
            Socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            Sender = Task.Run(ClientSender);

            //Register the Events
            OnAcceptReceived += OnAccept;
            OnDenyReceived += OnDeny;
            OnLogoutReceived += OnLogout;
            OnAckReceived += OnAck;

            try
            {
                if (IPAddress.TryParse(IP, out var ip))
                {
                    RemoteEndpoint = new IPEndPoint(ip, port);
                }
                else if (IP == "localhost")
                {
                    RemoteEndpoint = new IPEndPoint(IPAddress.Loopback, port);
                }
                else
                {
                    var addresses = await Dns.GetHostAddressesAsync(IP, CTS.Token).ConfigureAwait(false);
                    if (addresses.Length == 0) throw new ArgumentException("Unable to retrieve address from the specified host name.", nameof(IP));
                    RemoteEndpoint = new IPEndPoint(addresses[0], port);
                }
                
                ActivityChecker = Task.Run(ActivityCheck);
                Send(new Login() { Key = preferredKey, PositioningType = positioningType, Version = version });
                await ClientReceiveAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                OnFailed?.Invoke(this, new VoiceCraftErrorEventArgs(ex));
                await DisconnectAsync(ex.Message, false).ConfigureAwait(false);
            }
        }

        public async Task DisconnectAsync(string? reason = null, bool notifyServer = true)
        {
            ObjectDisposedException.ThrowIf(IsDisposed && State == VoiceCraftSocketState.Stopped, nameof(VoiceCraftSocket));
            if (State == VoiceCraftSocketState.Starting || State == VoiceCraftSocketState.Started) throw new InvalidOperationException("Cannot stop hosting as the socket is in a connection state.");
            OnLogoutReceived -= OnLogout;
            OnAckReceived -= OnAck;
            if(ClientNetPeer != null)
                ClientNetPeer.OnPacketReceived -= HandlePacketReceived;

            await CTS.CancelAsync().ConfigureAwait(false);
            CTS.Dispose();
            Socket.Close();
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            ClientNetPeer = null;
            ActivityChecker = null;
            Sender = null;
            State = VoiceCraftSocketState.Stopped;
            OnDisconnected?.Invoke(this, new VoiceCraftDisconnectedEventArgs(reason));
        }

        public async Task HostAsync(int Port)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, nameof(VoiceCraftSocket));
            if (State == VoiceCraftSocketState.Connected || State == VoiceCraftSocketState.Connecting) throw new InvalidOperationException("Cannot start hosting as socket is in a connection state!");
            if (State != VoiceCraftSocketState.Stopped) throw new InvalidOperationException("You must stop hosting before starting a host!");

            CTS.Dispose(); //Prevent memory leak for startup.
            //Socket.IOControl((IOControlCode)SioUdpConnReset, [0, 0, 0, 0], null); //I fucking hate this Windows Only

            State = VoiceCraftSocketState.Starting;
            CTS = new CancellationTokenSource();

            OnLoginReceived += OnClientLogin;
            OnLogoutReceived += OnClientLogout;
            OnPingReceived += OnPing;
            OnAckReceived += OnAck;

            try
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, Port);
                Socket.Bind(RemoteEndpoint);
                ActivityChecker = Task.Run(ServerCheck);
                Sender = Task.Run(ServerSender);
                State = VoiceCraftSocketState.Started;
                OnStarted?.Invoke(this, EventArgs.Empty);
                await ReceiveAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                OnFailed?.Invoke(this, new VoiceCraftErrorEventArgs(ex));
                await StopAsync(ex.Message).ConfigureAwait(false);
            }
        }

        public async Task StopAsync(string? reason = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed && State == VoiceCraftSocketState.Stopped, nameof(VoiceCraftSocket));
            if (State == VoiceCraftSocketState.Connecting || State == VoiceCraftSocketState.Connected) throw new InvalidOperationException("Cannot stop hosting as the socket is in a connection state.");
            if (State == VoiceCraftSocketState.Stopping || State == VoiceCraftSocketState.Stopped) return;

            State = VoiceCraftSocketState.Stopping;
            OnLoginReceived -= OnClientLogin;
            OnLogoutReceived -= OnClientLogout;
            OnPingReceived -= OnPing;
            OnAckReceived -= OnAck;

            DisconnectPeers("Server Shutdown.");

            while(!NetPeers.IsEmpty)
            {
                await Task.Delay(1).ConfigureAwait(false); //Wait until all peers are disconnected.
            }

            await CTS.CancelAsync().ConfigureAwait(false);
            CTS.Dispose();
            Socket.Close();
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            ActivityChecker = null;
            Sender = null;
            State = VoiceCraftSocketState.Stopped;
            OnStopped?.Invoke(this, new VoiceCraftStoppedEventArgs(reason));
        }

        public void Send(VoiceCraftPacket packet)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, nameof(VoiceCraftSocket));
            if (State == VoiceCraftSocketState.Connecting || State == VoiceCraftSocketState.Connected)
            {
                ClientNetPeer?.AddToSendBuffer(packet);
            }
            else
                throw new InvalidOperationException("Socket must be in a connecting or connected state to send packets!");
        }

        private void DisconnectPeers(string? reason = null)
        {
            foreach(var peerSocket in NetPeers)
            {
                peerSocket.Value.Disconnect(reason, true);
            }
        }

        private async Task SocketSendToAsync(VoiceCraftPacket packet, EndPoint ep)
        {
            var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(2048);
            try
            {
                packet.Write(buffer.AsSpan());
                // TODO: Optimize length. For now send 2048 bytes (MTU compliant receivers handle this)
                // Sending smaller length if possible would be better.
                await Socket.SendToAsync(new ArraySegment<byte>(buffer, 0, 2048), ep, CTS.Token).ConfigureAwait(false);
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private async Task SocketSendAsync(VoiceCraftPacket packet)
        {
            var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(2048);
            try
            {
                packet.Write(buffer.AsSpan());
                await Socket.SendToAsync(new ArraySegment<byte>(buffer, 0, 2048), RemoteEndpoint, CTS.Token).ConfigureAwait(false);
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private NetPeer CreateNetPeer(SocketAddress receivedAddress)
        {
            // Create an EndPoint from the SocketAddress
            var endpoint = RemoteEndpoint.Create(receivedAddress);
            var netPeer = new NetPeer(endpoint, long.MinValue, NetPeerState.Requesting);
            netPeer.OnPacketReceived += HandlePacketReceived;

            NetPeers.TryAdd(endpoint, netPeer);
            return netPeer;
        }

        private async Task ReceiveAsync()
        {
            byte[] buffer = GC.AllocateArray<byte>(length: 2048, pinned: true);
            Memory<byte> bufferMem = buffer.AsMemory();
            var receivedAddress = new SocketAddress(Socket.AddressFamily);

            while (!CTS.IsCancellationRequested)
            {
                try
                {
                    var receivedBytes = await Socket.ReceiveFromAsync(bufferMem, SocketFlags.None, receivedAddress, CTS.Token).ConfigureAwait(false);
                    
                    // Security: Basic Flood Protection
                    // In a real high-performance scenario, checking IP limits on every packet involves dictionary lookups.
                    // However, avoiding object creation (NetPeer) is the priority here.
                    
                    var receivedEndpoint = RemoteEndpoint.Create(receivedAddress);
                    NetPeers.TryGetValue(receivedEndpoint, out var netPeer);

                    if (netPeer?.State == NetPeerState.Connected)
                    {
                        // Existing connection logic - Fast path
                        var packet = PacketRegistry.GetPacketFromDataStream(bufferMem.Slice(0, receivedBytes).ToArray());

                        if (LogInbound && (InboundFilter.Count == 0 || InboundFilter.Contains((VoiceCraftPacketTypes)packet.PacketId)))
                            OnInboundPacket?.Invoke(this, new PacketEventArgs<VoiceCraftPacket>(packet, netPeer));

                        netPeer.AddToReceiveBuffer(packet); 
                    }
                    else
                    {
                        // New or Handshaking connection - Slow path with Security Checks
                        
                        // 1. Check Rate Limit / Blacklist before parsing (Optimized)
                        if (IsRateLimited(receivedAddress))
                        {
                            continue; // Drop packet silently to save resources
                        }

                        // 2. Parse Packet
                        var packet = PacketRegistry.GetPacketFromDataStream(bufferMem.Slice(0, receivedBytes).ToArray());

                        // 3. Only accept specific handshake packets for new peers
                        if(packet.PacketId == (byte)VoiceCraftPacketTypes.Login || packet.PacketId == (byte)VoiceCraftPacketTypes.PingInfo)
                        {
                            var peer = netPeer ?? CreateNetPeer(receivedAddress);

                            if (LogInbound && (InboundFilter.Count == 0 || InboundFilter.Contains((VoiceCraftPacketTypes)packet.PacketId)))
                                OnInboundPacket?.Invoke(this, new PacketEventArgs<VoiceCraftPacket>(packet, peer));

                            peer.AddToReceiveBuffer(packet);
                        }
                    }
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.ConnectionReset || ex.SocketErrorCode == SocketError.ConnectionAborted || ex.SocketErrorCode == SocketError.TimedOut) continue;
                    await StopAsync(ex.Message).ConfigureAwait(false);
                    return;
                }
                catch(OperationCanceledException)
                {
                    return;
                }
                catch(Exception ex)
                {
                    if (LogExceptions)
                        OnExceptionError?.Invoke(this, new VoiceCraftErrorEventArgs(ex));
                }
            }
        }

        private async Task ClientReceiveAsync()
        {
            byte[] buffer = GC.AllocateArray<byte>(length: 2048, pinned: true);
            Memory<byte> bufferMem = buffer.AsMemory();

            while (!CTS.IsCancellationRequested)
            {
                try
                {
                    var result = await Socket.ReceiveFromAsync(bufferMem, SocketFlags.None, RemoteEndpoint, CTS.Token).ConfigureAwait(false);
                    var packet = PacketRegistry.GetPacketFromDataStream(bufferMem.Slice(0, result.ReceivedBytes).ToArray());

                    ClientNetPeer?.AddToReceiveBuffer(packet); //We don't care about wether the client is connected or not, We'll just accept the packet into the buffer.

                    if (ClientNetPeer != null && LogInbound && (InboundFilter.Count == 0 || InboundFilter.Contains((VoiceCraftPacketTypes)packet.PacketId)))
                        OnInboundPacket?.Invoke(this, new PacketEventArgs<VoiceCraftPacket>(packet, ClientNetPeer));
                }
                catch (SocketException ex)
                {
                    await DisconnectAsync(ex.Message, false).ConfigureAwait(false);
                    return;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                if (LogExceptions)
                        OnExceptionError?.Invoke(this, new VoiceCraftErrorEventArgs(ex));
                }
            }
        }

        private async Task ActivityCheck()
        {
            var time = Environment.TickCount64;
            while (!CTS.IsCancellationRequested)
            {
                var dist = Environment.TickCount64 - ClientNetPeer?.LastActive;
                if (dist > Timeout)
                {
                    await DisconnectAsync($"Connection timed out!\nTime since last active {Environment.TickCount64 - ClientNetPeer?.LastActive}ms.", false).ConfigureAwait(false);
                    break;
                }
                ClientNetPeer?.ResendPackets();

                if (Environment.TickCount64 - time >= 1000) //1 second ping interval.
                {
                    Send(new Ping());
                    time = Environment.TickCount64;
                }
                await Task.Delay(100).ConfigureAwait(false);
            }
        }

        private async Task ServerCheck()
        {
            while (!CTS.IsCancellationRequested)
            {
                for (int i = NetPeers.Count - 1; i >= 0; i--)
                {
                    var peer = NetPeers.ElementAt(i);
                    var diff = Environment.TickCount64 - peer.Value.LastActive;
                    if ((diff > Timeout || diff < 0) && peer.Value.State != NetPeerState.Disconnected) //Negative values are pretty much invalid.
                    {
                        peer.Value.Disconnect($"Timeout - Last Active: {Environment.TickCount64 - peer.Value.LastActive}ms", true);
                    }
                }

                foreach (var peer in NetPeers)
                {
                    peer.Value.ResendPackets();
                }

                // Security: Cleanup Rate Limits to prevent memory leak
                if (Environment.TickCount64 % 10000 < 100) // Run roughly every 10 seconds
                {
                    var now = Environment.TickCount64;
                    var keysToRemove = new List<string>();
                    foreach(var kvp in _rateLimits)
                    {
                        if (now - kvp.Value.LastReset > 5000) // Remove entries older than 5 seconds
                        {
                            keysToRemove.Add(kvp.Key);
                        }
                    }
                    foreach(var key in keysToRemove)
                    {
                        _rateLimits.TryRemove(key, out _);
                    }
                }

                await Task.Delay(100).ConfigureAwait(false);
            }
        }

        private async Task ServerSender()
        {
            var tasks = new List<Task>();
            while (!CTS.IsCancellationRequested)
            {
                tasks.Clear();
                foreach (var peer in NetPeers)
                {
                    var maxSendTime = Environment.TickCount64 + MaxSendTime;
                    while (peer.Value.SendQueue.TryDequeue(out VoiceCraftPacket? packet) && Environment.TickCount64 < maxSendTime && !CTS.IsCancellationRequested)
                    {
                        if (packet.Retries > NetPeer.MaxSendRetries)
                        {
                            peer.Value.Disconnect("Unstable Connection.", true);
                            continue;
                        }
                        
                        tasks.Add(SocketSendToAsync(packet, peer.Value.RemoteEndPoint));

                        if (LogOutbound && (OutboundFilter.Count == 0 || OutboundFilter.Contains((VoiceCraftPacketTypes)packet.PacketId)))
                            OnOutboundPacket?.Invoke(this, new PacketEventArgs<VoiceCraftPacket>(packet, peer.Value));
                        
                        // Optimization: Dispose packet if it uses pooled resources
                        if (packet is IDisposable disposable)
                             disposable.Dispose();
                    }

                    if (peer.Value.State == NetPeerState.Disconnected)
                    {
                        NetPeers.TryRemove(peer);
                        OnPeerDisconnected?.Invoke(this, new PacketEventArgs<string?>(peer.Value.DisconnectReason, peer.Value));
                    }
                }
                
                if (tasks.Count > 0)
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                else
                {
                await Task.Delay(1).ConfigureAwait(false);
                }
            }
        }

        private async Task ClientSender()
        {
            while (!CTS.IsCancellationRequested && ClientNetPeer != null)
            {
                while (ClientNetPeer.SendQueue.TryDequeue(out VoiceCraftPacket? packet) && !CTS.IsCancellationRequested)
                {
                    if (packet.Retries > NetPeer.MaxSendRetries)
                    {
                        await DisconnectAsync("Unstable Connection.").ConfigureAwait(false);
                        continue;
                    }
                    await SocketSendAsync(packet).ConfigureAwait(false);

                    if (LogOutbound && (OutboundFilter.Count == 0 || OutboundFilter.Contains((VoiceCraftPacketTypes)packet.PacketId)))
                        OnOutboundPacket?.Invoke(this, new PacketEventArgs<VoiceCraftPacket>(packet, ClientNetPeer));

                    // Optimization: Dispose packet if it uses pooled resources
                    if (packet is IDisposable disposable)
                        disposable.Dispose();
                }

                await Task.Delay(1).ConfigureAwait(false);
            }
        }

        private long GetAvailableId()
        {
            var Id = NetPeer.GenerateId();
            while(IdExists(Id))
            {
                Id = NetPeer.GenerateId();
            }
            return Id;
        }

        private bool IdExists(long id)
        {
            foreach(var peer in NetPeers)
            {
                if(peer.Value.Id == id) return true;
            }
            return false;
        }

        private bool IsRateLimited(SocketAddress address)
        {
            // Simple key based on string representation of address (IP+Port usually, but here we want IP based preferably)
            // SocketAddress doesn't give easy IP access without parsing, but for UDP, the full address (Endpoint) is the peer.
            // If we want to limit by IP, we need to extract it.
            // For now, limiting by EndPoint (SocketAddress) prevents a single port spam, but not a distributed attack or port rotation.
            // However, parsing IP from SocketAddress every packet is expensive.
            // Let's rely on the fact that standard clients reuse ports or behave predictably.
            // A persistent attacker rotating ports will still fill this dictionary, so we need cleanup.

            var updates = _rateLimits.AddOrUpdate(address.ToString(), 
                new RateLimitInfo { Count = 1, LastReset = Environment.TickCount64 },
                (key, current) => 
                {
                    var now = Environment.TickCount64;
                     if (now - current.LastReset > 1000)
                    {
                        current.Count = 1;
                        current.LastReset = now;
                    }
                    else
                    {
                        current.Count++;
                    }
                    return current;
                });

            return updates.Count > PacketsPerSecondLimit;
        }

        private sealed class RateLimitInfo
        {
            public int Count;
            public long LastReset;
        }

        private void HandlePacketReceived(object? sender, PacketEventArgs<VoiceCraftPacket> e)
        {
            var packet = e.Packet;
            var peer = (NetPeer?)e.Source;
            
            if (peer == null) return;

            switch ((VoiceCraftPacketTypes)packet.PacketId)
            {
                case VoiceCraftPacketTypes.Login: OnLoginReceived?.Invoke(this, new PacketEventArgs<Login>((Login)packet, peer)); break;
                case VoiceCraftPacketTypes.Logout: OnLogoutReceived?.Invoke(this, new PacketEventArgs<Logout>((Logout)packet, peer)); break;
                case VoiceCraftPacketTypes.Accept: OnAcceptReceived?.Invoke(this, new PacketEventArgs<Accept>((Accept)packet, peer)); break;
                case VoiceCraftPacketTypes.Deny: OnDenyReceived?.Invoke(this, new PacketEventArgs<Deny>((Deny)packet, peer)); break;
                case VoiceCraftPacketTypes.Ack: OnAckReceived?.Invoke(this, new PacketEventArgs<Ack>((Ack)packet, peer)); break;
                case VoiceCraftPacketTypes.Ping: OnPingReceived?.Invoke(this, new PacketEventArgs<Ping>((Ping)packet, peer)); break;
                case VoiceCraftPacketTypes.PingInfo: OnPingInfoReceived?.Invoke(this, new PacketEventArgs<PingInfo>((PingInfo)packet, peer)); break;
                case VoiceCraftPacketTypes.Binded: OnBindedReceived?.Invoke(this, new PacketEventArgs<Binded>((Binded)packet, peer)); break;
                case VoiceCraftPacketTypes.Unbinded: OnUnbindedReceived?.Invoke(this, new PacketEventArgs<Unbinded>((Unbinded)packet, peer)); break;
                case VoiceCraftPacketTypes.ParticipantJoined: OnParticipantJoinedReceived?.Invoke(this, new PacketEventArgs<ParticipantJoined>((ParticipantJoined)packet, peer)); break;
                case VoiceCraftPacketTypes.ParticipantLeft: OnParticipantLeftReceived?.Invoke(this, new PacketEventArgs<ParticipantLeft>((ParticipantLeft)packet, peer)); break;
                case VoiceCraftPacketTypes.Mute: OnMuteReceived?.Invoke(this, new PacketEventArgs<Mute>((Mute)packet, peer)); break;
                case VoiceCraftPacketTypes.Unmute: OnUnmuteReceived?.Invoke(this, new PacketEventArgs<Unmute>((Unmute)packet, peer)); break;
                case VoiceCraftPacketTypes.Deafen: OnDeafenReceived?.Invoke(this, new PacketEventArgs<Deafen>((Deafen)packet, peer)); break;
                case VoiceCraftPacketTypes.Undeafen: OnUndeafenReceived?.Invoke(this, new PacketEventArgs<Undeafen>((Undeafen)packet, peer)); break;
                case VoiceCraftPacketTypes.JoinChannel: OnJoinChannelReceived?.Invoke(this, new PacketEventArgs<JoinChannel>((JoinChannel)packet, peer)); break;
                case VoiceCraftPacketTypes.LeaveChannel: OnLeaveChannelReceived?.Invoke(this, new PacketEventArgs<LeaveChannel>((LeaveChannel)packet, peer)); break;
                case VoiceCraftPacketTypes.AddChannel: OnAddChannelReceived?.Invoke(this, new PacketEventArgs<AddChannel>((AddChannel)packet, peer)); break;
                case VoiceCraftPacketTypes.RemoveChannel: OnRemoveChannelReceived?.Invoke(this, new PacketEventArgs<RemoveChannel>((RemoveChannel)packet, peer)); break;
                case VoiceCraftPacketTypes.UpdatePosition: OnUpdatePositionReceived?.Invoke(this, new PacketEventArgs<UpdatePosition>((UpdatePosition)packet, peer)); break;
                case VoiceCraftPacketTypes.FullUpdatePosition: OnFullUpdatePositionReceived?.Invoke(this, new PacketEventArgs<FullUpdatePosition>((FullUpdatePosition)packet, peer)); break;
                case VoiceCraftPacketTypes.UpdateEnvironmentId: OnUpdateEnvironmentIdReceived?.Invoke(this, new PacketEventArgs<UpdateEnvironmentId>((UpdateEnvironmentId)packet, peer)); break;
                case VoiceCraftPacketTypes.ClientAudio: OnClientAudioReceived?.Invoke(this, new PacketEventArgs<ClientAudio>((ClientAudio)packet, peer)); break;
                case VoiceCraftPacketTypes.ServerAudio: OnServerAudioReceived?.Invoke(this, new PacketEventArgs<ServerAudio>((ServerAudio)packet, peer)); break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (State == VoiceCraftSocketState.Started || State == VoiceCraftSocketState.Starting)
                    StopAsync().GetAwaiter().GetResult();

                if (State == VoiceCraftSocketState.Connected || State == VoiceCraftSocketState.Connecting)
                    DisconnectAsync().GetAwaiter().GetResult();

                Socket.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Client Event Methods
        private void OnAccept(object? sender, PacketEventArgs<Accept> e)
        {
            if (ClientNetPeer != null)
            {
                ClientNetPeer.Id = e.Packet.Id;
                IsConnected = true;
            }
            State = VoiceCraftSocketState.Connected;
            OnConnected?.Invoke(this, new VoiceCraftConnectedEventArgs(e.Packet.Key));
        }

        private async void OnDeny(object? sender, PacketEventArgs<Deny> e)
        {
            if (!IsConnected)
                await DisconnectAsync(e.Packet.Reason, false).ConfigureAwait(false);
        }

        private async void OnLogout(object? sender, PacketEventArgs<Logout> e)
        {
            await DisconnectAsync(e.Packet.Reason, false).ConfigureAwait(false);
        }
        #endregion

        #region Server Event Methods
        private void OnClientLogin(object? sender, PacketEventArgs<Login> e)
        {
            var peer = (NetPeer)e.Source;
            if (peer.State == NetPeerState.Connected)
            {
                peer.AddToSendBuffer(new Accept());
                return; //Already Connected
            }

            var Id = GetAvailableId();

            peer.Id = Id;
            OnPeerConnected?.Invoke(this, new PacketEventArgs<Login>(e.Packet, peer)); //Leave wether the client should be accepted or denied by the application.
        }

        private void OnClientLogout(object? sender, PacketEventArgs<Logout> e)
        {
            ((NetPeer)e.Source).Disconnect(null, false);
        }

        private void OnPing(object? sender, PacketEventArgs<Ping> e)
        {
            ((NetPeer)e.Source).AddToSendBuffer(new Ping());
        }
        #endregion

        #region Global Event Methods
        private void OnAck(object? sender, PacketEventArgs<Ack> e)
        {
            ((NetPeer)e.Source).AcknowledgePacket(e.Packet.PacketSequence);
        }
        #endregion
    }

    public enum VoiceCraftSocketState
    {
        Stopped,

        //Client
        Connecting,
        Connected,
        Disconnecting,

        //Hoster
        Starting,
        Started,
        Stopping
    }

    public class VoiceCraftConnectedEventArgs : EventArgs
    {
        public short Key { get; }
        public VoiceCraftConnectedEventArgs(short key) => Key = key;
    }

    public class VoiceCraftDisconnectedEventArgs : EventArgs
    {
        public string? Reason { get; }
        public VoiceCraftDisconnectedEventArgs(string? reason) => Reason = reason;
    }

    public class VoiceCraftStoppedEventArgs : EventArgs
    {
        public string? Reason { get; }
        public VoiceCraftStoppedEventArgs(string? reason) => Reason = reason;
    }

    public class VoiceCraftErrorEventArgs : EventArgs
    {
        public Exception Error { get; }
        public VoiceCraftErrorEventArgs(Exception error) => Error = error;
    }
