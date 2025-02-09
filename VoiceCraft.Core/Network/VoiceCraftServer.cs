using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;
using VoiceCraft.Core.Data;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Core.Network
{
    public class VoiceCraftServer : IDisposable
    {
        // ReSharper disable once InconsistentNaming
        private const int PINGER_BROADCAST_INTERVAL_MS = 5000;

        public static readonly Version Version = new Version(1, 1, 0);

        public event Action? OnStarted;
        public event Action? OnStopped;
        public event Action<NetPeer>? OnClientConnected;
        public event Action<NetPeer, DisconnectInfo>? OnClientDisconnected;
        public event Action<VoiceCraftEntity>? OnEntityCreated;

        //Server Properties
        public string Motd { get; set; } = "VoiceCraft Proximity Chat!";
        public bool DiscoveryEnabled { get; set; }
        public PositioningType PositioningType { get; set; }
        
        //Public Properties
        public List<VoiceCraftEntity> Entities { get; set; } = new List<VoiceCraftEntity>();

        private readonly EventBasedNetListener _listener;
        private readonly NetManager _netManager;
        private readonly CancellationTokenSource _cts;
        private readonly NetDataWriter _dataWriter;
        private bool _isDisposed;
        private int _lastPingBroadcast = Environment.TickCount;

        public VoiceCraftServer()
        {
            _dataWriter = new NetDataWriter();
            _cts = new CancellationTokenSource();
            _listener = new EventBasedNetListener();
            _netManager = new NetManager(_listener)
            {
                AutoRecycle = true
            };

            _listener.ConnectionRequestEvent += OnConnectionRequestEvent;
            _listener.PeerConnectedEvent += ListenerOnPeerConnectedEvent;
            _listener.PeerDisconnectedEvent += ListenerOnPeerDisconnectedEvent;
            _listener.NetworkReceiveEvent += ListenerOnNetworkReceiveEvent;
        }

        ~VoiceCraftServer()
        {
            Dispose(false);
        }

        //Public Methods
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
            var serverInfoPacket = new ServerInfoPacket()
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

        public void Stop()
        {
            if (!_netManager.IsRunning) return;
            _netManager.Stop();
            OnStopped?.Invoke();
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

        public void AddEntity(VoiceCraftEntity entity)
        {
            var networkEntities = Entities.Where(x => x is NetworkEntity).Cast<NetworkEntity>().Select(x => x.Peer).ToArray();
            
            foreach (var networkEntity in networkEntities)
            {
                var packet = new EntityCreatedPacket(); //Finish this
                SendPacket(networkEntity, packet);
            }
            
            Entities.Add(entity);
            var entityCreatedPacket = new EntityCreatedPacket(); //Finish this.
            SendPacket(networkEntities, entityCreatedPacket);
            OnEntityCreated?.Invoke(entity);
        }

        //Events
        private void OnConnectionRequestEvent(ConnectionRequest request)
        {
            if (request.Data.IsNull)
            {
                request.Reject();
                return;
            }

            try
            {
                var loginPacket = new LoginPacket();
                loginPacket.Deserialize(request.Data);
                OnLoginPacketReceived(loginPacket, request);
            }
            catch
            {
                request.Reject();
            }
        }

        private void ListenerOnPeerConnectedEvent(NetPeer peer)
        {
            OnClientConnected?.Invoke(peer);
            if ((LoginType?)peer.Tag != LoginType.Pinger) return;
            var serverInfoPacket = new ServerInfoPacket()
            {
                Motd = Motd,
                Discovery = DiscoveryEnabled,
                PositioningType = PositioningType,
            };
            SendPacket(peer, serverInfoPacket);
        }

        private void ListenerOnPeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectinfo)
        {
            OnClientDisconnected?.Invoke(peer, disconnectinfo);
        }

        private void ListenerOnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliverymethod)
        {
            var packetType = reader.GetByte();
            var pt = (PacketType)packetType;
            switch (pt)
            {
                case PacketType.EntityAudio:
                    //Unused Packet Types.
                case PacketType.Login:
                case PacketType.ServerInfo:
                case PacketType.EntityCreated:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            reader.Recycle();
        }
        
        //Packet Events
        private void OnLoginPacketReceived(LoginPacket loginPacket, ConnectionRequest request)
        {
            if (Version.Parse(loginPacket.Version).Major != Version.Major)
            {
                request.Reject();
                return;
            }
                
            switch (loginPacket.LoginType)
            {
                case LoginType.Pinger:
                    var pingerPeer = request.Accept();
                    pingerPeer.Tag = loginPacket.LoginType;
                    break;
                case LoginType.Login:
                    var loginPeer = request.Accept();
                    loginPeer.Tag = loginPacket.LoginType;
                    AddEntity(new NetworkEntity(loginPeer));
                    break;
                case LoginType.Discovery:
                    var discoveryPeer = request.Accept();
                    discoveryPeer.Tag = loginPacket.LoginType;
                    break;
                default:
                    request.Reject();
                    break;
            }
        }

        private void OnAudioPacketReceived(NetPeer peer, EntityAudioPacket entityAudioPacket)
        {
            
        }

        //Dispose
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
    }
}