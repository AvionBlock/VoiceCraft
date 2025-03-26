using System.Diagnostics;
using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.Packets;
using VoiceCraft.Server.Application;
using VoiceCraft.Server.Config;

namespace VoiceCraft.Server.Systems
{
    public class NetworkSystem
    {
        private readonly NetDataWriter _dataWriter;
        private readonly VoiceCraftWorld _world;
        private readonly EventBasedNetListener _listener;
        private readonly NetManager _netManager;
        private readonly VoiceCraftConfig _config;

        public NetworkSystem(VoiceCraftServer server, NetManager netManager)
        {
            _dataWriter = new NetDataWriter();
            _world = server.World;
            _listener = server.Listener;
            _config = server.Config;
            _netManager = netManager;

            _listener.PeerConnectedEvent += OnPeerConnectedEvent;
            _listener.PeerDisconnectedEvent += OnPeerDisconnectedEvent;
            _listener.ConnectionRequestEvent += OnConnectionRequest;
            _listener.NetworkReceiveEvent += OnNetworkReceiveEvent;
            _listener.NetworkReceiveUnconnectedEvent += OnNetworkReceiveUnconnectedEvent;
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
        
        public bool SendUnconnectedPacket<T>(IPEndPoint remoteEndPoint, T packet) where T : VoiceCraftPacket
        {
            _dataWriter.Reset();
            _dataWriter.Put((byte)packet.PacketType);
            packet.Serialize(_dataWriter);
            return _netManager.SendUnconnectedMessage(_dataWriter, remoteEndPoint);
        }

        public void Broadcast<T>(T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered) where T : VoiceCraftPacket
        {
            _dataWriter.Reset();
            _dataWriter.Put((byte)packet.PacketType);
            packet.Serialize(_dataWriter);

            foreach (var client in _netManager.ConnectedPeerList.Where(x => x.Tag is VoiceCraftNetworkEntity))
            {
                client.Send(_dataWriter, deliveryMethod);
            }
        }
        
        public void SendEntityEffects(VoiceCraftEntity entity, VoiceCraftNetworkEntity targetEntity)
        {
            foreach (var effect in entity.Effects)
            {
                var addEffectPacket = new AddEffectPacket(entity.Id, effect.Value.EffectType);
                SendPacket(targetEntity.NetPeer, addEffectPacket);
            }
        }

        public void SendEntityEffectUpdates(VoiceCraftEntity entity, VoiceCraftNetworkEntity targetEntity)
        {
            foreach (var effect in entity.Effects)
            {
                var updateEffectPacket = new UpdateEffectPacket(entity.Id, effect.Value);
                SendPacket(targetEntity.NetPeer, updateEffectPacket);
            }
        }

        public void SendEntityData(VoiceCraftEntity entity, VoiceCraftNetworkEntity targetEntity)
        {
            var updatePositionPacket = new UpdatePositionPacket(entity.Id, entity.Position);
            var updateRotationPacket = new UpdateRotationPacket(entity.Id, entity.Rotation);

            SendPacket(targetEntity.NetPeer, updatePositionPacket);
            SendPacket(targetEntity.NetPeer, updateRotationPacket);
        }
        
        private void OnPeerConnectedEvent(NetPeer peer)
        {
            if (peer.Tag is not VoiceCraftNetworkEntity networkEntity) return;
            foreach (var visibleEntity in _world.Entities)
            {
                SendEntityEffects(visibleEntity.Value, networkEntity);
            }
        }
        
        private void OnPeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectinfo)
        {
            if (peer.Tag != (object)LoginType.Login) return;
            _world.DestroyEntity(peer.Id);
        }

        private void OnConnectionRequest(ConnectionRequest request)
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
                if (Version.Parse(loginPacket.Version).Major != VoiceCraftServer.Version.Major)
                {
                    request.Reject("Incompatible client/server version!"u8.ToArray());
                    return;
                }
                
                HandleLogin(loginPacket, request);
            }
            catch
            {
                request.Reject("An error occurred on the server while trying to parse the login!"u8.ToArray());
            }
        }

        private void HandleLogin(LoginPacket loginPacket, ConnectionRequest request)
        {
            switch (loginPacket.LoginType)
            {
                case LoginType.Login:
                    var loginPeer = request.Accept();
                    loginPeer.Tag = _world.CreateEntity(loginPeer);
                    break;
                case LoginType.Discovery:
                    var peer = request.Accept();
                    peer.Tag = LoginType.Discovery;
                    break;
                default:
                    request.Reject();
                    break;
            }
        }

        private void OnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliverymethod)
        {
            try
            {
                var packetType = reader.GetByte();
                var pt = (PacketType)packetType;
                switch (pt)
                {
                    case PacketType.Audio:
                        var audioPacket = new AudioPacket(0, [], 0, 0);
                        audioPacket.Deserialize(reader);
                        HandleAudioPacket(audioPacket, peer);
                        break;
                    // Will need to implement these for client sided mode later.
                    case PacketType.Info:
                    case PacketType.Login:
                    case PacketType.EntityCreated:
                    case PacketType.EntityDestroyed:
                    case PacketType.UpdatePosition:
                    case PacketType.UpdateRotation:
                    case PacketType.UpdateTalkBitmask:
                    case PacketType.UpdateListenBitmask:
                    case PacketType.UpdateName:
                    case PacketType.AddEffect:
                    case PacketType.UpdateEffect:
                    case PacketType.RemoveEffect:
                    case PacketType.Unknown:
                    default:
                        break;
                }

                reader.Recycle();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void OnNetworkReceiveUnconnectedEvent(IPEndPoint remoteendpoint, NetPacketReader reader, UnconnectedMessageType messagetype)
        {
            try
            {
                var packetType = reader.GetByte();
                var pt = (PacketType)packetType;
                switch (pt)
                {
                    case PacketType.Info:
                        var infoPacket = new InfoPacket();
                        infoPacket.Deserialize(reader);
                        HandleInfoPacket(infoPacket, remoteendpoint);
                        break;
                    //Unused
                    case PacketType.Login:
                    case PacketType.Audio:
                    case PacketType.EntityCreated:
                    case PacketType.EntityDestroyed:
                    case PacketType.UpdatePosition:
                    case PacketType.UpdateRotation:
                    case PacketType.UpdateTalkBitmask:
                    case PacketType.UpdateListenBitmask:
                    case PacketType.UpdateName:
                    case PacketType.AddEffect:
                    case PacketType.UpdateEffect:
                    case PacketType.RemoveEffect:
                    case PacketType.Unknown:
                    default:
                        break;
                }

                reader.Recycle();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        
        //Packet Handling
        private void HandleInfoPacket(InfoPacket infoPacket, IPEndPoint remoteendpoint)
        {
            infoPacket.Clients = _netManager.ConnectedPeersCount;
            infoPacket.Discovery = _config.Discovery;
            infoPacket.PositioningType = _config.PositioningType;
            infoPacket.Motd = _config.Motd;
            SendUnconnectedPacket(remoteendpoint, infoPacket);
        }

        private void HandleAudioPacket(AudioPacket audioPacket, NetPeer peer)
        {
            if (!_world.Entities.TryGetValue(audioPacket.NetworkId, out var entity)) return;
            if (entity is not VoiceCraftNetworkEntity networkEntity || !Equals(networkEntity.NetPeer, peer)) return;
            networkEntity.ReceiveAudio(audioPacket.Data, audioPacket.Timestamp);
        }
    }
}