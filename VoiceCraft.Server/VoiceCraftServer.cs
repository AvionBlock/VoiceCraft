using Friflo.Engine.ECS;
using LiteNetLib;
using LiteNetLib.Utils;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.Packets;
using VoiceCraft.Server.EventHandlers;

namespace VoiceCraft.Server
{
    public class VoiceCraftServer : IDisposable
    {
        // ReSharper disable once InconsistentNaming
        private const int PINGER_BROADCAST_INTERVAL_MS = 5000;

        public static readonly Version Version = new(1, 1, 0);

        public event Action? OnStarted;
        public event Action? OnStopped;
        //Server Properties
        public string Motd { get; set; } = "VoiceCraft Proximity Chat!";
        public bool DiscoveryEnabled { get; set; }
        public PositioningType PositioningType { get; set; }

        //Public Properties
        public EventBasedNetListener Listener { get; }
        public EntityStore World { get; }
        
        private readonly NetManager _netManager;
        private readonly CancellationTokenSource _cts;
        private readonly NetDataWriter _dataWriter;
        private readonly NetworkEventHandler _networkEventHandler;
        private readonly PacketEventHandler _packetHandler;
        private readonly WorldEventHandler _worldEventHandler;
        private bool _isDisposed;
        private int _lastPingBroadcast = Environment.TickCount;

        public VoiceCraftServer()
        {
            _dataWriter = new NetDataWriter();
            _cts = new CancellationTokenSource();
            Listener = new EventBasedNetListener();
            _netManager = new NetManager(Listener)
            {
                AutoRecycle = true
            };
            World = new EntityStore();
            _networkEventHandler = new NetworkEventHandler(this);
            _packetHandler = new PacketEventHandler(this);
            _worldEventHandler = new WorldEventHandler(this);
        }

        ~VoiceCraftServer()
        {
            Dispose(false);
        }

        #region Public Methods

        public void Start(int port)
        {
            if (_netManager.IsRunning) return;
            _netManager.Start(port);
            OnStarted?.Invoke();
        }

        public void Update()
        {
            _netManager.PollEvents();

            if (Environment.TickCount - _lastPingBroadcast < PINGER_BROADCAST_INTERVAL_MS) return;
            _lastPingBroadcast = Environment.TickCount;
            var serverInfoPacket = new InfoPacket()
            {
                Motd = Motd,
                Discovery = DiscoveryEnabled,
                PositioningType = PositioningType,
            };

            SendPacket(
                _netManager.ConnectedPeerList
                    .Where(peer => peer.ConnectionState == ConnectionState.Connected && (LoginType?)peer.Tag == LoginType.Pinger).ToArray(),
                serverInfoPacket);
        }
        
        public bool SendPacket<T>(NetPeer peer, T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered) where T : VoiceCraftPacket
        {
            if (peer.ConnectionState != ConnectionState.Connected) return false;

            _dataWriter.Reset();
            _dataWriter.Put((byte)packet.PacketType);
            packet.Serialize(_dataWriter);
            peer.Send(_dataWriter, deliveryMethod);
            return true;
        }

        public bool SendPacket<T>(NetPeer[] peers, T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered) where T : VoiceCraftPacket
        {
            var status = true;
            foreach (var peer in peers)
            {
                status = SendPacket(peer, packet, deliveryMethod);
            }

            return status;
        }

        public void Stop()
        {
            if (!_netManager.IsRunning) return;
            _netManager.Stop();
            OnStopped?.Invoke();
        }

        #endregion

        #region Dispose
        private void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                _netManager.Stop();
                _cts.Cancel();
                _cts.Dispose();
            }

            _isDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}