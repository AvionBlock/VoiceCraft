using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Core.Network
{
    public class VoiceCraftServer : IDisposable
    {
        // ReSharper disable once InconsistentNaming
        private const int PINGER_BROADCAST_INTERVAL_MS = 5000;
        
        public static readonly Version Version = Version.Parse("1.2.0.0");

        public event Action? OnStarted;
        public event Action? OnStopped;
        public event Action<NetPeer>? OnClientConnected;
        public event Action<NetPeer, DisconnectInfo>? OnClientDisconnected;

        public uint UpdateIntervalMs { get; set; } = 10;
        public string Motd { get; set; } = "VoiceCraft Proximity Chat!";
        
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
                var lastPingBroadcast = Environment.TickCount;
                
                while (!token.IsCancellationRequested)
                {
                    _netManager.PollEvents();
                    Task.Delay(TimeSpan.FromMilliseconds(UpdateIntervalMs)).Wait(); 
                    
                    if (Environment.TickCount - lastPingBroadcast < PINGER_BROADCAST_INTERVAL_MS) continue;
                    lastPingBroadcast = Environment.TickCount;
                    foreach (var peer in _netManager.ConnectedPeerList.Where(peer => peer.ConnectionState == ConnectionState.Connected && (ConnectionType?)peer.Tag == ConnectionType.Pinger))
                    {
                        var serverInfoPacket = new ServerInfoPacket()
                        {
                            Motd = Motd
                        };
                        
                        SendPacket(peer, serverInfoPacket);
                    }
                }
            });
            
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

        public void Stop()
        {
            if (!_netManager.IsRunning) return;
            _netManager.Stop();
            OnStopped?.Invoke();
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
        private void OnConnectionRequestEvent(ConnectionRequest request)
        {
            if (request.Data.IsNull)
            {
                request.Reject();
                return;
            }
            
            var connectionType = (ConnectionType)request.Data.GetInt();
            switch (connectionType)
            {
                case ConnectionType.Pinger:
                case ConnectionType.Login:
                case ConnectionType.Discovery:
                    var peer = request.Accept();
                    peer.Tag = connectionType;
                    break;
                default:
                    request.Reject();
                    break;
            }
        }
        
        private void ListenerOnPeerConnectedEvent(NetPeer peer)
        {
            OnClientConnected?.Invoke(peer);
            if((ConnectionType?)peer.Tag == ConnectionType.Pinger)
                SendPacket(peer, new ServerInfoPacket { Motd = Motd });
        }
        
        private void ListenerOnPeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectinfo)
        {
            OnClientDisconnected?.Invoke(peer, disconnectinfo);
        }
        
        private void ListenerOnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliverymethod)
        {
            _packetProcessor.ReadAllPackets(reader, peer);
            reader.Recycle();
        }
        
        //Packet Events

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