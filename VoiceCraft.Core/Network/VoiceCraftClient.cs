using System;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network
{
    public class VoiceCraftClient : IDisposable
    {
        public int Ping { get; private set; }
        public ConnectionStatus Status { get; private set; }

        public event Action? OnConnected;
        public event Action<DisconnectInfo>? OnDisconnected;
        public event Action<int>? OnLatencyUpdated;
        
        private readonly EventBasedNetListener _listener;
        private readonly NetManager _netManager;
        private readonly NetPacketProcessor _packetProcessor;
        private readonly CancellationTokenSource _cts;
        private NetPeer? _serverPeer;
        private bool _isDisposed;

        public VoiceCraftClient()
        {
            _packetProcessor = new NetPacketProcessor();
            _cts = new CancellationTokenSource();
            _listener = new EventBasedNetListener();
            _netManager = new NetManager(_listener)
            {
                AutoRecycle = true,
                IPv6Enabled = false
            };
            
            var token = _cts.Token;
            Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    _netManager.PollEvents();
                    Task.Delay(1).Wait();
                }
            }, _cts.Token);
            
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

        public void Connect(string ip, int port, ConnectionType connectionType)
        {
            ThrowIfDisposed();
            if(Status != ConnectionStatus.Disconnected)
                throw new InvalidOperationException("You must disconnect before connecting!");
            
            if(!_netManager.IsRunning)
                _netManager.Start();
            
            Status = ConnectionStatus.Connecting;
            _netManager.Connect(ip, port, "VoiceCraft");
        }

        public void Disconnect()
        {
            ThrowIfDisposed();
            if(Status == ConnectionStatus.Disconnected)
                throw new InvalidOperationException("Must be connecting or connected before disconnecting!");
            
            _netManager.DisconnectPeer(_serverPeer);
        }

        //Events
        private void OnPeerConnectedEvent(NetPeer peer)
        {
            Status = ConnectionStatus.Connected;
            _serverPeer = peer;
            OnConnected?.Invoke();
        }
        
        private void OnPeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Status = ConnectionStatus.Disconnected;
            _serverPeer = null;
            OnDisconnected?.Invoke(disconnectInfo);
        }
        
        private void OnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliverymethod)
        {
            reader.Recycle(); //Temporary
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