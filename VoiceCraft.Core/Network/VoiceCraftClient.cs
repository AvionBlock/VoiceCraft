using System;
using LiteNetLib;
using LiteNetLib.Utils;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Core.Network
{
    public class VoiceCraftClient : IDisposable
    {
        public static readonly Version Version = new Version(1, 1, 0);
        
        public int Ping { get; private set; }
        public ConnectionStatus ConnectionStatus { get; private set; }

        //Network Events
        public event Action? OnConnected;
        public event Action<DisconnectInfo>? OnDisconnected;
        public event Action<int>? OnLatencyUpdated;
        
        //Packet Events
        public event Action<ServerInfoPacket>? OnServerInfoPacketReceived;
        
        private readonly EventBasedNetListener _listener;
        private readonly NetManager _netManager;
        private readonly NetDataWriter _dataWriter;
        private NetPeer? _serverPeer;
        private bool _isDisposed;

        public VoiceCraftClient()
        {
            _dataWriter = new NetDataWriter();
            _listener = new EventBasedNetListener();
            _netManager = new NetManager(_listener)
            {
                AutoRecycle = true,
                IPv6Enabled = false
            };
            
            _listener.PeerConnectedEvent += OnPeerConnectedEvent;
            _listener.PeerDisconnectedEvent += OnPeerDisconnectedEvent;
            _listener.NetworkReceiveEvent += OnNetworkReceiveEvent;
            _listener.NetworkLatencyUpdateEvent += OnNetworkLatencyUpdateEvent;
            _listener.ConnectionRequestEvent += OnConnectionRequestEvent;
        }

        ~VoiceCraftClient()
        {
            Dispose(false);
        }

        public void Connect(string ip, int port, LoginType loginType)
        {
            ThrowIfDisposed();
            if(ConnectionStatus != ConnectionStatus.Disconnected)
                throw new InvalidOperationException("You must disconnect before connecting!");
            
            if(!_netManager.IsRunning)
                _netManager.Start();
            
            ConnectionStatus = ConnectionStatus.Connecting;
            var loginPacket = new LoginPacket()
            {
                Version = Version.ToString(),
                LoginType = loginType,
                PositioningType = PositioningType.Server
            };
            _dataWriter.Reset();
            loginPacket.Serialize(_dataWriter);
            _netManager.Connect(ip, port, _dataWriter);
        }

        public void Update()
        {
            _netManager.PollEvents();
        }

        public void Disconnect()
        {
            ThrowIfDisposed();
            if(ConnectionStatus == ConnectionStatus.Disconnected)
                throw new InvalidOperationException("Must be connecting or connected before disconnecting!");
            
            if(_serverPeer != null)
                _netManager.DisconnectPeer(_serverPeer);
            
            Update();
        }
        
        public bool SendPacket<T>(NetPeer peer, T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered) where T : VoiceCraftPacket
        {
            if(peer.ConnectionState != ConnectionState.Connected) return false;
            
            _dataWriter.Reset();
            _dataWriter.Put((byte)packet.PacketType);
            packet.Serialize(_dataWriter);
            peer.Send(_dataWriter, deliveryMethod);
            return true;
        }

        //Events
        private void OnPeerConnectedEvent(NetPeer peer)
        {
            ConnectionStatus = ConnectionStatus.Connected;
            _serverPeer = peer;
            OnConnected?.Invoke();
        }
        
        private void OnPeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            ConnectionStatus = ConnectionStatus.Disconnected;
            _serverPeer = null;
            OnDisconnected?.Invoke(disconnectInfo);
        }
        
        private void OnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliverymethod)
        {
            var packetType = reader.GetByte();
            var pt = (PacketType)packetType;
            switch (pt)
            {
                case PacketType.ServerInfo:
                    var serverInfoPacket = new ServerInfoPacket();
                    serverInfoPacket.Deserialize(reader);
                    OnServerInfoPacketReceive(serverInfoPacket, peer);
                    break;
                case PacketType.Login:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private void OnNetworkLatencyUpdateEvent(NetPeer peer, int latency)
        {
            Ping = latency;
            OnLatencyUpdated?.Invoke(latency);
        }
        
        private static void OnConnectionRequestEvent(ConnectionRequest request)
        {
            request.Reject();
        }
        
        //Packet Events
        private void OnServerInfoPacketReceive(ServerInfoPacket packet, NetPeer peer)
        {
            OnServerInfoPacketReceived?.Invoke(packet);
        }

        private void ThrowIfDisposed()
        {
            if (!_isDisposed) return;
            throw new ObjectDisposedException(typeof(VoiceCraftClient).ToString());
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                _netManager.Stop();
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