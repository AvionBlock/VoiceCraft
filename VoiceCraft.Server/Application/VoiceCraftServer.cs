using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Network.Packets;
using VoiceCraft.Server.Config;
using VoiceCraft.Server.Data;
using VoiceCraft.Server.Systems;

namespace VoiceCraft.Server.Application
{
    public class VoiceCraftServer : IResettable, IDisposable
    {
        public static readonly Version Version = new(1, 1, 0);

        //Public Properties
        public VoiceCraftConfig Config { get; private set; }
        public VoiceCraftWorld World { get; }

        //Privates
        //Networking
        private readonly NetDataWriter _dataWriter;
        private readonly NetManager _netManager;

        //Systems
        private readonly NetworkSystem _networkSystem;
        private readonly AudioEffectSystem _audioEffectSystem;
        private readonly VisibilitySystem _visibilitySystem;
        private readonly EventHandlerSystem _eventHandlerSystem;
        private bool _isDisposed;

        public VoiceCraftServer()
        {
            Config = new VoiceCraftConfig();
            World = new VoiceCraftWorld();

            _dataWriter = new NetDataWriter();
            var listener = new EventBasedNetListener();
            _netManager = new NetManager(listener)
            {
                AutoRecycle = true,
                UnconnectedMessagesEnabled = true
            };

            _audioEffectSystem = new AudioEffectSystem();
            _networkSystem = new NetworkSystem(this, World, listener, _netManager);
            _eventHandlerSystem = new EventHandlerSystem(this, World, _audioEffectSystem);
            _visibilitySystem = new VisibilitySystem(World, _audioEffectSystem);
        }

        ~VoiceCraftServer()
        {
            Dispose(false);
        }
        
        public bool Start(VoiceCraftConfig? config = null)
        {
            Config = config ?? new VoiceCraftConfig();
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

        public void Broadcast<T>(T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered, params NetPeer?[] excludes)
            where T : VoiceCraftPacket
        {
            lock (_dataWriter)
            {
                var networkEntities = World.Entities.OfType<VoiceCraftNetworkEntity>();
                _dataWriter.Reset();
                _dataWriter.Put((byte)packet.PacketType);
                packet.Serialize(_dataWriter);
                foreach (var networkEntity in networkEntities)
                {
                    if (excludes.Contains(networkEntity.NetPeer)) continue;
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

        public void Reset()
        {
            World.Reset();
            _audioEffectSystem.Reset();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        private void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                World.Dispose();
                _netManager.Stop();
                _networkSystem.Dispose();
                _audioEffectSystem.Dispose();
                _eventHandlerSystem.Dispose();
            }

            _isDisposed = true;
        }
    }
}