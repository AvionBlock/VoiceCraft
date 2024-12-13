using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network
{
    public class VoiceCraftServer : IDisposable
    {
        public static readonly Version Version = Version.Parse("1.2.0.0");

        public event Action? OnStarted;
        public event Action? OnStopped;
        public event Action<NetPeer>? OnClientConnected;
        public event Action<NetPeer, DisconnectInfo>? OnClientDisconnected;
        
        private readonly EventBasedNetListener _listener;
        private readonly NetManager _netManager;
        private readonly NetPacketProcessor _packetProcessor;
        private readonly CancellationTokenSource _cts;
        private readonly NetDataWriter _dataWriter;
        private bool _isDisposed;

        public VoiceCraftServer()
        {
            _dataWriter = new NetDataWriter();
            _packetProcessor = new NetPacketProcessor();
            _cts = new CancellationTokenSource();
            _listener = new EventBasedNetListener();
            _netManager = new NetManager(_listener)
            {
                AutoRecycle = true
            };
            
            var token = _cts.Token;
            Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    _netManager.PollEvents();
                    Task.Delay(15).Wait();
                }
            });
            
            _listener.ConnectionRequestEvent += OnConnectionRequestEvent;
            _listener.PeerConnectedEvent += ListenerOnPeerConnectedEvent;
            _listener.PeerDisconnectedEvent += ListenerOnPeerDisconnectedEvent;
        }

        ~VoiceCraftServer()
        {
            Dispose(false);
        }

        public void Start(int port)
        {
            if (_netManager.IsRunning) return;
            _netManager.Start(port);
            OnStarted?.Invoke();
        }

        public void Stop()
        {
            if (!_netManager.IsRunning) return;
            _netManager.Stop();
            OnStopped?.Invoke();
        }
        
        private void OnConnectionRequestEvent(ConnectionRequest request)
        {
            request.Accept(); //Temporary
        }
        
        private void ListenerOnPeerConnectedEvent(NetPeer peer)
        {
            OnClientConnected?.Invoke(peer);
        }
        
        private void ListenerOnPeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectinfo)
        {
            OnClientDisconnected?.Invoke(peer, disconnectinfo);
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