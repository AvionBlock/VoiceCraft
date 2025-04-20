using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Core.Network.Packets;
using VoiceCraft.Server.Config;
using VoiceCraft.Server.Data;
using VoiceCraft.Server.Systems;

namespace VoiceCraft.Server.Application
{
    public class VoiceCraftServer : IDisposable
    {
        public static readonly Version Version = new(1, 1, 0);

        //Public Properties
        public VoiceCraftConfig Config { get; set; }
        public EventBasedNetListener Listener { get; }
        public VoiceCraftWorld World { get; } = new();
        public NetworkSystem NetworkSystem { get; }
        public AudioEffectSystem AudioEffectSystem { get; }
        
        //Privates
        private readonly NetDataWriter _dataWriter = new();
        private readonly VisibilitySystem _visibilitySystem;
        private readonly EventHandlerSystem _eventHandlerSystem;
        private readonly NetManager _netManager;
        private bool _isDisposed;

        public VoiceCraftServer(VoiceCraftConfig? config = null)
        {
            Config = config ?? new VoiceCraftConfig();
            Listener = new EventBasedNetListener();
            _netManager = new NetManager(Listener)
            {
                AutoRecycle = true,
                UnconnectedMessagesEnabled = true
            };

            //Has to be initialized in this order otherwise shit falls apart.
            NetworkSystem = new NetworkSystem(this, _netManager);
            AudioEffectSystem = new AudioEffectSystem();
            _visibilitySystem = new VisibilitySystem(this);
            _eventHandlerSystem = new EventHandlerSystem(this); //This should always be last!
        }

        ~VoiceCraftServer()
        {
            Dispose(false);
        }

        #region Public Methods
        public bool Start()
        {
            return _netManager.IsRunning || _netManager.Start((int)Config.Port);
        }
        
        public bool SendPacket<T>(NetPeer peer, T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered) where T : VoiceCraftPacket
        {
            if (peer.ConnectionState != ConnectionState.Connected) return false;

            lock (_dataWriter)
            {
                _dataWriter.Reset();
                _dataWriter.Put((byte)packet.PacketType);
                packet.Serialize(_dataWriter);
                peer.Send(_dataWriter, deliveryMethod);
                return true;
            }
        }

        public bool SendPacket<T>(NetPeer[] peers, T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered) where T : VoiceCraftPacket
        {
            lock (_dataWriter)
            {
                _dataWriter.Reset();
                _dataWriter.Put((byte)packet.PacketType);
                packet.Serialize(_dataWriter);

                var status = true;
                foreach (var peer in peers)
                {
                    if (peer.ConnectionState != ConnectionState.Connected)
                    {
                        status = false;
                        continue;
                    }

                    peer.Send(_dataWriter, deliveryMethod);
                }
                return status;
            }
        }
        
        public bool SendUnconnectedPacket<T>(IPEndPoint remoteEndPoint, T packet) where T : VoiceCraftPacket
        {
            lock (_dataWriter)
            {
                _dataWriter.Reset();
                _dataWriter.Put((byte)packet.PacketType);
                packet.Serialize(_dataWriter);
                return _netManager.SendUnconnectedMessage(_dataWriter, remoteEndPoint);
            }
        }
        
        public void Broadcast<T>(T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered, params NetPeer?[] excludes) where T : VoiceCraftPacket
        {
            lock (_dataWriter)
            {
                var networkEntities = World.Entities.OfType<VoiceCraftNetworkEntity>();
                _dataWriter.Reset();
                _dataWriter.Put((byte)packet.PacketType);
                packet.Serialize(_dataWriter);
                foreach (var networkEntity in networkEntities)
                {
                    if(excludes.Contains(networkEntity.NetPeer)) continue;
                    networkEntity.NetPeer.Send(_dataWriter, deliveryMethod);
                }
            }
        }

        public void Update()
        {
            _netManager.PollEvents();
            _visibilitySystem.Update();
            _eventHandlerSystem.Update();
        }

        public void Stop()
        {
            if (!_netManager.IsRunning) return;
            _netManager.DisconnectAll();
            _netManager.Stop();
        }

        #endregion

        #region Dispose

        private void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                _netManager.Stop();
                World.Dispose();
                NetworkSystem.Dispose();
                _eventHandlerSystem.Dispose();
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