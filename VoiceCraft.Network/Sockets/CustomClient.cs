using System.Net;
using System.Net.Sockets;
using System.Numerics;
using VoiceCraft.Core;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Packets.CustomClient;

namespace VoiceCraft.Network.Sockets
{
    public class CustomClient : Disposable
    {
        public const int SIO_UDP_CONNRESET = -1744830452;

        #region Variables
        public int Timeout { get; set; } = 8000;
        public CustomClientSocketState State { get; private set; }
        public IPEndPoint RemoteEndpoint { get; private set; } = new IPEndPoint(IPAddress.Any, 0);
        public Socket Socket { get; set; } = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        private PacketRegistry PacketRegistry { get; set; } = new();
        private CancellationTokenSource CTS { get; set; } = new();
        private SocketAddress? RemoteAddress { get; set; }
        private Task? ActivityChecker { get; set; }
        private long LastActive { get; set; }
        #endregion

        #region Debug
        public bool LogExceptions { get; set; } = false;
        public bool LogInbound { get; set; } = false;
        public bool LogOutbound { get; set; } = false;
        public List<CustomClientTypes> InboundFilter { get; set; } = [];
        public List<CustomClientTypes> OutboundFilter { get; set; } = [];
        #endregion

        #region Delegates
        public delegate void Started();
        public delegate void Stopped(string? reason = null);
        public delegate void Connected(string name);
        public delegate void Disconnected();
        public delegate void Updated(Vector3 position, float rotation, float caveDensity, bool isUnderwater, string dimensionId, string levelId, string serverId);
        public delegate void PacketData<T>(T data, SocketAddress address);

        //Error and Debug Events
        public delegate void OutboundPacket(CustomClientPacket packet);
        public delegate void InboundPacket(CustomClientPacket packet);
        public delegate void ExceptionError(Exception error);
        public delegate void Failed(Exception ex);
        #endregion

        #region Events
        public event Started? OnStarted;
        public event Stopped? OnStopped;
        public event Connected? OnConnected;
        public event Disconnected? OnDisconnected;
        public event Updated? OnUpdated;

        public event PacketData<Login>? OnLoginReceived;
        public event PacketData<Logout>? OnLogoutReceived;
        public event PacketData<Accept>? OnAcceptReceived;
        public event PacketData<Deny>? OnDenyReceived;
        public event PacketData<Update>? OnUpdateReceived;

        //Error and Debug Events
        public event OutboundPacket? OnOutboundPacket;
        public event InboundPacket? OnInboundPacket;
        public event ExceptionError? OnExceptionError;
        public event Failed? OnFailed;
        #endregion

        public CustomClient()
        {
            PacketRegistry.RegisterPacket((byte)CustomClientTypes.Login, typeof(Login));
            PacketRegistry.RegisterPacket((byte)CustomClientTypes.Logout, typeof(Logout));
            PacketRegistry.RegisterPacket((byte)CustomClientTypes.Accept, typeof(Accept));
            PacketRegistry.RegisterPacket((byte)CustomClientTypes.Deny, typeof(Deny));
            PacketRegistry.RegisterPacket((byte)CustomClientTypes.Update, typeof(Update));
        }

        #region Methods
        public async Task HostAsync(int Port)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            if (State != CustomClientSocketState.Stopped) throw new Exception("You must stop hosting before starting a host!");

            CTS.Dispose(); //Prevent memory leak for startup.
            //Socket.IOControl((IOControlCode)SIO_UDP_CONNRESET, [0, 0, 0, 0], null); //I fucking hate this

            State = CustomClientSocketState.Starting;
            CTS = new CancellationTokenSource();

            OnLoginReceived += LoginReceived;
            OnLogoutReceived += LogoutReceived;
            OnUpdateReceived += UpdateReceived;

            try
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, Port);
                Socket.Bind(RemoteEndpoint);
                ActivityChecker = Task.Run(CheckerLoop);
                State = CustomClientSocketState.Started;
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
            ObjectDisposedException.ThrowIf(IsDisposed && State == CustomClientSocketState.Stopped, this);
            if (State == CustomClientSocketState.Stopped || State == CustomClientSocketState.Stopping) return;
            State = CustomClientSocketState.Stopping;

            if(RemoteAddress != null)
                await SocketSendToAsync(new Logout(), RemoteAddress);

            CTS.Cancel();
            CTS.Dispose();
            Socket.Close();
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            ActivityChecker = null;
            State = CustomClientSocketState.Stopped;
            OnStopped?.Invoke(reason);
        }

        private async Task SocketSendToAsync(CustomClientPacket packet, SocketAddress address)
        {
            var buffer = new List<byte>();
            packet.WritePacket(ref buffer);
            await Socket.SendToAsync(buffer.ToArray(),SocketFlags.None, address, CTS.Token);

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
                    var receivedBytes = await Socket.ReceiveFromAsync(bufferMem, SocketFlags.None, receivedAddress, CTS.Token);
                    var packet = PacketRegistry.GetCustomPacketFromDataStream(bufferMem.ToArray());

                    if (LogInbound && (InboundFilter.Count == 0 || InboundFilter.Contains((CustomClientTypes)packet.PacketId)))
                        OnInboundPacket?.Invoke(packet);

                    HandlePacketReceived(receivedAddress, packet);
                }
                catch (SocketException ex)
                {
                    await StopAsync(ex.Message);
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

        private void HandlePacketReceived(SocketAddress address, CustomClientPacket packet)
        {
            switch ((CustomClientTypes)packet.PacketId)
            {
                case CustomClientTypes.Login: OnLoginReceived?.Invoke((Login)packet, address); break;
                case CustomClientTypes.Logout: OnLogoutReceived?.Invoke((Logout)packet, address); break;
                case CustomClientTypes.Accept: OnAcceptReceived?.Invoke((Accept)packet, address); break;
                case CustomClientTypes.Deny: OnDenyReceived?.Invoke((Deny)packet, address); break;
                case CustomClientTypes.Update: OnUpdateReceived?.Invoke((Update)packet, address); break;
            }
        }

        private async Task CheckerLoop()
        {
            while (!CTS.IsCancellationRequested)
            {
                var dist = Environment.TickCount64 - LastActive;
                if (RemoteAddress != null && dist > Timeout)
                {
                    RemoteAddress = null;
                    OnDisconnected?.Invoke();
                    break;
                }

                await Task.Delay(100).ConfigureAwait(false);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (State == CustomClientSocketState.Started || State == CustomClientSocketState.Starting)
                    StopAsync().Wait();

                Socket.Dispose();
            }
        }
        #endregion

        #region Event Methods
        private async void LoginReceived(Login data, SocketAddress address)
        {
            if(RemoteAddress != null && !RemoteAddress.Equals(address))
            {
                await SocketSendToAsync(new Deny() { Reason = "Client is already connected to another instance!" }, address);
                return;
            }

            LastActive = Environment.TickCount64;
            RemoteAddress = new SocketAddress(address.Family, address.Size);
            await SocketSendToAsync(new Accept(), address);
            OnConnected?.Invoke(data.Name);
        }

        private async void LogoutReceived(Logout data, SocketAddress address)
        {
            if(RemoteAddress != null && RemoteAddress.Equals(address))
            {
                RemoteAddress = null;
                await SocketSendToAsync(new Accept(), address);
                OnDisconnected?.Invoke();
            }
            return;
        }

        private async void UpdateReceived(Update data, SocketAddress address)
        {
            if (RemoteAddress != null && RemoteAddress.Equals(address))
            {
                LastActive = Environment.TickCount64;
                await SocketSendToAsync(new Accept(), address);
                OnUpdated?.Invoke(data.Position, data.Rotation, data.CaveDensity, data.IsUnderwater, data.DimensionId, data.LevelId, data.ServerId);
            }
            return;
        }
        #endregion
    }
    public enum CustomClientSocketState
    {
        Stopped,
        Stopping,
        Starting,
        Started
    }
}
