using System.Net.Sockets;
using System.Net;
using VoiceCraft.Core;
using VoiceCraft.Core.Packets;
using System.Collections.Concurrent;
using VoiceCraft.Core.Packets.VoiceCraft;

namespace VoiceCraft.Network.Sockets
{
    public class VoiceCraft : Disposable
    {
        public const long MaxSendTime = 100;
        public const int SIO_UDP_CONNRESET = -1744830452;
        #region Variables
        //Public Variables
        public PacketRegistry PacketRegistry { get; set; } = new PacketRegistry();
        public Socket Socket { get; private set; } = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        public IPEndPoint RemoteEndpoint { get; private set; } = new IPEndPoint(IPAddress.Any, 0);
        public VoiceCraftSocketState State { get; private set; }
        public int Timeout { get; set; } = 8000;
        public bool IsConnected { get; private set; }

        //Private Variables
        private CancellationTokenSource CTS { get; set; } = new CancellationTokenSource();
        private ConcurrentDictionary<EndPoint, NetPeer> NetPeers { get; set; } = new(); //Server Variable
        private ConcurrentDictionary<string, RateLimitInfo> _rateLimits = new(); // Security: Rate Limiting
        private const int MaxConnectionsPerIP = 5;
        private const int PacketsPerSecondLimit = 10;
        private NetPeer? ClientNetPeer { get; set; } //Client Variable
        private Task? ActivityChecker { get; set; }
        private Task? Sender { get; set; }
        #endregion

        public VoiceCraft()
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
        public bool LogExceptions { get; set; } = false;
        public bool LogInbound { get; set; } = false;
        public bool LogOutbound { get; set; } = false;
        public List<VoiceCraftPacketTypes> InboundFilter { get; set; } = [];
        public List<VoiceCraftPacketTypes> OutboundFilter { get; set; } = [];
        #endregion

        #region Delegates
        public delegate void Connected(short key);
        public delegate void Disconnected(string? reason = null);

        public delegate void Started();
        public delegate void Stopped(string? reason = null);
        public delegate void PeerConnected(NetPeer peer, Login packet);
        public delegate void PeerDisconnected(NetPeer peer, string? reason = null);

        public delegate void PacketData<T>(T data, NetPeer peer);

        //Error and Debug Events
        public delegate void OutboundPacket(VoiceCraftPacket packet, NetPeer peer);
        public delegate void InboundPacket(VoiceCraftPacket packet, NetPeer peer);
        public delegate void ExceptionError(Exception error);
        public delegate void Failed(Exception ex);
        #endregion

        #region Events
        //Client Events
        public event Connected? OnConnected;
        public event Disconnected? OnDisconnected;

        //Server Events
        public event Started? OnStarted;
        public event Stopped? OnStopped;
        public event PeerConnected? OnPeerConnected;
        public event PeerDisconnected? OnPeerDisconnected;

        //Packet Events
        public event PacketData<Login>? OnLoginReceived;
        public event PacketData<Logout>? OnLogoutReceived;
        public event PacketData<Accept>? OnAcceptReceived;
        public event PacketData<Deny>? OnDenyReceived;
        public event PacketData<Ack>? OnAckReceived;
        public event PacketData<Ping>? OnPingReceived;
        public event PacketData<PingInfo>? OnPingInfoReceived;
        public event PacketData<Binded>? OnBindedReceived;
        public event PacketData<Unbinded>? OnUnbindedReceived;
        public event PacketData<ParticipantJoined>? OnParticipantJoinedReceived;
        public event PacketData<ParticipantLeft>? OnParticipantLeftReceived;
        public event PacketData<Mute>? OnMuteReceived;
        public event PacketData<Unmute>? OnUnmuteReceived;
        public event PacketData<Deafen>? OnDeafenReceived;
        public event PacketData<Undeafen>? OnUndeafenReceived;
        public event PacketData<JoinChannel>? OnJoinChannelReceived;
        public event PacketData<LeaveChannel>? OnLeaveChannelReceived;
        public event PacketData<AddChannel>? OnAddChannelReceived;
        public event PacketData<RemoveChannel>? OnRemoveChannelReceived;
        public event PacketData<UpdatePosition>? OnUpdatePositionReceived;
        public event PacketData<FullUpdatePosition>? OnFullUpdatePositionReceived;
        public event PacketData<UpdateEnvironmentId>? OnUpdateEnvironmentIdReceived;
        public event PacketData<ClientAudio>? OnClientAudioReceived;
        public event PacketData<ServerAudio>? OnServerAudioReceived;

        //Error and Debug Events
        public event OutboundPacket? OnOutboundPacket;
        public event InboundPacket? OnInboundPacket;
        public event ExceptionError? OnExceptionError;
        public event Failed? OnFailed;
        #endregion

        #region Methods
        public async Task ConnectAsync(string IP, int port, short preferredKey, PositioningTypes positioningType, string version)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, nameof(VoiceCraft));
            if (State == VoiceCraftSocketState.Started || State == VoiceCraftSocketState.Starting) throw new Exception("Cannot start connection as socket is in a hosting state!");
            if (State != VoiceCraftSocketState.Stopped) throw new Exception("You must disconnect before reconnecting!");

            CTS.Dispose(); //Prevent memory leak for startup.
            //Socket.IOControl((IOControlCode)SIO_UDP_CONNRESET, [0, 0, 0, 0], null); //I fucking hate this Windows Only

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
                    var addresses = await Dns.GetHostAddressesAsync(IP, CTS.Token);
                    if (addresses.Length == 0) throw new ArgumentException("Unable to retrieve address from the specified host name.", nameof(IP));
                    RemoteEndpoint = new IPEndPoint(addresses[0], port);
                }
                
                ActivityChecker = Task.Run(ActivityCheck);
                Send(new Login() { Key = preferredKey, PositioningType = positioningType, Version = version });
                await ClientReceiveAsync();
            }
            catch (Exception ex)
            {
                OnFailed?.Invoke(ex);
                await DisconnectAsync(ex.Message, false);
            }
        }

        public async Task DisconnectAsync(string? reason = null, bool notifyServer = true)
        {
            ObjectDisposedException.ThrowIf(IsDisposed && State == VoiceCraftSocketState.Stopped, nameof(VoiceCraft));
            if (State == VoiceCraftSocketState.Starting || State == VoiceCraftSocketState.Started) throw new InvalidOperationException("Cannot stop hosting as the socket is in a connection state.");
            if (State == VoiceCraftSocketState.Stopped || State == VoiceCraftSocketState.Disconnecting) return;

            //We don't need to wait until we are connected because the Cancellation Token already takes care of cancelling other thread related requests.

            if (notifyServer && State == VoiceCraftSocketState.Connected) //Only send if we are connected.
                await SocketSendAsync(new Logout() { Id = ClientNetPeer?.Id ?? long.MinValue });

            State = VoiceCraftSocketState.Disconnecting;
            //Deregister the Events
            OnAcceptReceived -= OnAccept;
            OnDenyReceived -= OnDeny;
            OnLogoutReceived -= OnLogout;
            OnAckReceived -= OnAck;
            if(ClientNetPeer != null)
                ClientNetPeer.OnPacketReceived -= HandlePacketReceived;

            CTS.Cancel();
            CTS.Dispose();
            Socket.Close();
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            ClientNetPeer = null;
            ActivityChecker = null;
            Sender = null;
            State = VoiceCraftSocketState.Stopped;
            OnDisconnected?.Invoke(reason);
        }

        public async Task HostAsync(int Port)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, nameof(VoiceCraft));
            if (State == VoiceCraftSocketState.Connected || State == VoiceCraftSocketState.Connecting) throw new Exception("Cannot start hosting as socket is in a connection state!");
            if (State != VoiceCraftSocketState.Stopped) throw new Exception("You must stop hosting before starting a host!");

            CTS.Dispose(); //Prevent memory leak for startup.
            //Socket.IOControl((IOControlCode)SIO_UDP_CONNRESET, [0, 0, 0, 0], null); //I fucking hate this Windows Only

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
                OnStarted?.Invoke();
                await ReceiveAsync();
            }
            catch (Exception ex)
            {
                OnFailed?.Invoke(ex);
                await StopAsync(ex.Message);
            }
        }

        public async Task StopAsync(string? reason = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed && State == VoiceCraftSocketState.Stopped, nameof(VoiceCraft));
            if (State == VoiceCraftSocketState.Connecting || State == VoiceCraftSocketState.Connected) throw new InvalidOperationException("Cannot stop hosting as the socket is in a connection state.");
            if (State == VoiceCraftSocketState.Stopping || State == VoiceCraftSocketState.Stopped) return;

            State = VoiceCraftSocketState.Stopping;
            OnLoginReceived -= OnClientLogin;
            OnLogoutReceived -= OnClientLogout;
            OnPingReceived -= OnPing;
            OnAckReceived -= OnAck;

            DisconnectPeers("Server Shutdown.");

            while(NetPeers.Count > 0)
            {
                await Task.Delay(1); //Wait until all peers are disconnected.
            }

            CTS.Cancel();
            CTS.Dispose();
            Socket.Close();
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            ActivityChecker = null;
            Sender = null;
            State = VoiceCraftSocketState.Stopped;
            OnStopped?.Invoke(reason);
        }

        public void Send(VoiceCraftPacket packet)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, nameof(VoiceCraft));
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
            var buffer = new List<byte>();
            packet.WritePacket(ref buffer);
            await Socket.SendToAsync(buffer.ToArray(), ep, CTS.Token);
        }

        private async Task SocketSendAsync(VoiceCraftPacket packet)
        {
            var buffer = new List<byte>();
            packet.WritePacket(ref buffer);
            await Socket.SendToAsync(buffer.ToArray(), RemoteEndpoint, CTS.Token);
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
                    var receivedBytes = await Socket.ReceiveFromAsync(bufferMem, SocketFlags.None, receivedAddress, CTS.Token);
                    
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
                            OnInboundPacket?.Invoke(packet, netPeer);

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
                                OnInboundPacket?.Invoke(packet, peer);

                            peer.AddToReceiveBuffer(packet);
                        }
                    }
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.ConnectionReset || ex.SocketErrorCode == SocketError.ConnectionAborted || ex.SocketErrorCode == SocketError.TimedOut) continue;
                    await StopAsync(ex.Message);
                    return;
                }
                catch(OperationCanceledException)
                {
                    return;
                }
                catch(Exception ex)
                {
                    if (LogExceptions)
                        OnExceptionError?.Invoke(ex);
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
                    var result = await Socket.ReceiveFromAsync(bufferMem, SocketFlags.None, RemoteEndpoint, CTS.Token);
                    var packet = PacketRegistry.GetPacketFromDataStream(bufferMem.Slice(0, result.ReceivedBytes).ToArray());

                    ClientNetPeer?.AddToReceiveBuffer(packet); //We don't care about wether the client is connected or not, We'll just accept the packet into the buffer.

                    if (ClientNetPeer != null && LogInbound && (InboundFilter.Count == 0 || InboundFilter.Contains((VoiceCraftPacketTypes)packet.PacketId)))
                        OnInboundPacket?.Invoke(packet, ClientNetPeer);
                }
                catch (SocketException ex)
                {
                    await DisconnectAsync(ex.Message, false); //Socket is basically closed at this point, We can't send a message.
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

        private async Task ActivityCheck()
        {
            var time = Environment.TickCount64;
            while (!CTS.IsCancellationRequested)
            {
                var dist = Environment.TickCount64 - ClientNetPeer?.LastActive;
                if (dist > Timeout)
                {
                    await DisconnectAsync($"Connection timed out!\nTime since last active {Environment.TickCount64 - ClientNetPeer?.LastActive}ms.", false);
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
                            OnOutboundPacket?.Invoke(packet, peer.Value);
                        
                        // Optimization: Dispose packet if it uses pooled resources
                        if (packet is IDisposable disposable)
                             disposable.Dispose();
                    }

                    if (peer.Value.State == NetPeerState.Disconnected)
                    {
                        NetPeers.TryRemove(peer);
                        OnPeerDisconnected?.Invoke(peer.Value, peer.Value.DisconnectReason);
                    }
                }
                
                if (tasks.Count > 0)
                {
                    await Task.WhenAll(tasks);
                }
                else
                {
                    await Task.Delay(1); //1ms to not destroy the CPU.
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
                        await DisconnectAsync("Unstable Connection.");
                        continue;
                    }
                    await SocketSendAsync(packet);

                    if (LogOutbound && (OutboundFilter.Count == 0 || OutboundFilter.Contains((VoiceCraftPacketTypes)packet.PacketId)))
                        OnOutboundPacket?.Invoke(packet, ClientNetPeer);

                    // Optimization: Dispose packet if it uses pooled resources
                    if (packet is IDisposable disposable)
                        disposable.Dispose();
                }

                await Task.Delay(1); //1ms to not destroy the CPU.
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

        private class RateLimitInfo
        {
            public int Count;
            public long LastReset;
        }

        private void HandlePacketReceived(NetPeer peer, VoiceCraftPacket packet)
        {
            switch ((VoiceCraftPacketTypes)packet.PacketId)
            {
                case VoiceCraftPacketTypes.Login: OnLoginReceived?.Invoke((Login)packet, peer); break;
                case VoiceCraftPacketTypes.Logout: OnLogoutReceived?.Invoke((Logout)packet, peer); break;
                case VoiceCraftPacketTypes.Accept: OnAcceptReceived?.Invoke((Accept)packet, peer); break;
                case VoiceCraftPacketTypes.Deny: OnDenyReceived?.Invoke((Deny)packet, peer); break;
                case VoiceCraftPacketTypes.Ack: OnAckReceived?.Invoke((Ack)packet, peer); break;
                case VoiceCraftPacketTypes.Ping: OnPingReceived?.Invoke((Ping)packet, peer); break;
                case VoiceCraftPacketTypes.PingInfo: OnPingInfoReceived?.Invoke((PingInfo)packet, peer); break;
                case VoiceCraftPacketTypes.Binded: OnBindedReceived?.Invoke((Binded)packet, peer); break;
                case VoiceCraftPacketTypes.Unbinded: OnUnbindedReceived?.Invoke((Unbinded)packet, peer); break;
                case VoiceCraftPacketTypes.ParticipantJoined: OnParticipantJoinedReceived?.Invoke((ParticipantJoined)packet, peer); break;
                case VoiceCraftPacketTypes.ParticipantLeft: OnParticipantLeftReceived?.Invoke((ParticipantLeft)packet, peer); break;
                case VoiceCraftPacketTypes.Mute: OnMuteReceived?.Invoke((Mute)packet, peer); break;
                case VoiceCraftPacketTypes.Unmute: OnUnmuteReceived?.Invoke((Unmute)packet, peer); break;
                case VoiceCraftPacketTypes.Deafen: OnDeafenReceived?.Invoke((Deafen)packet, peer); break;
                case VoiceCraftPacketTypes.Undeafen: OnUndeafenReceived?.Invoke((Undeafen)packet, peer); break;
                case VoiceCraftPacketTypes.JoinChannel: OnJoinChannelReceived?.Invoke((JoinChannel)packet, peer); break;
                case VoiceCraftPacketTypes.LeaveChannel: OnLeaveChannelReceived?.Invoke((LeaveChannel)packet, peer); break;
                case VoiceCraftPacketTypes.AddChannel: OnAddChannelReceived?.Invoke((AddChannel)packet, peer); break;
                case VoiceCraftPacketTypes.RemoveChannel: OnRemoveChannelReceived?.Invoke((RemoveChannel)packet, peer); break;
                case VoiceCraftPacketTypes.UpdatePosition: OnUpdatePositionReceived?.Invoke((UpdatePosition)packet, peer); break;
                case VoiceCraftPacketTypes.FullUpdatePosition: OnFullUpdatePositionReceived?.Invoke((FullUpdatePosition)packet, peer); break;
                case VoiceCraftPacketTypes.UpdateEnvironmentId: OnUpdateEnvironmentIdReceived?.Invoke((UpdateEnvironmentId)packet, peer); break;
                case VoiceCraftPacketTypes.ClientAudio: OnClientAudioReceived?.Invoke((ClientAudio)packet, peer); break;
                case VoiceCraftPacketTypes.ServerAudio: OnServerAudioReceived?.Invoke((ServerAudio)packet, peer); break;
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
        }
        #endregion

        #region Client Event Methods
        private void OnAccept(Accept data, NetPeer peer)
        {
            if(ClientNetPeer != null)
            {
                ClientNetPeer.Id = data.Id;
                IsConnected = true;
            }
            State = VoiceCraftSocketState.Connected;
            OnConnected?.Invoke(data.Key);
        }

        private async void OnDeny(Deny data, NetPeer peer)
        {
            if(!IsConnected)
                await DisconnectAsync(data.Reason, false);
        }

        private async void OnLogout(Logout data, NetPeer peer)
        {
            await DisconnectAsync(data.Reason, false);
        }
        #endregion

        #region Server Event Methods
        private void OnClientLogin(Login data, NetPeer peer)
        {
            if (peer.State == NetPeerState.Connected)
            {
                peer.AddToSendBuffer(new Accept());
                return; //Already Connected
            }

            var Id = GetAvailableId();

            peer.Id = Id;
            OnPeerConnected?.Invoke(peer, data); //Leave wether the client should be accepted or denied by the application.
        }

        private void OnClientLogout(Logout data, NetPeer peer)
        {
            peer.Disconnect(null, false);
        }

        private void OnPing(Ping data, NetPeer peer)
        {
            peer.AddToSendBuffer(new Ping());
        }
        #endregion

        #region Global Event Methods
        private void OnAck(Ack data, NetPeer peer)
        {
            peer.AcknowledgePacket(data.PacketSequence);
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
}