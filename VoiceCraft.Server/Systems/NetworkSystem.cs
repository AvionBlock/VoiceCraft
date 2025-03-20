using System.Diagnostics;
using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.Packets;
using VoiceCraft.Server.Application;

namespace VoiceCraft.Server.Systems
{
    public class NetworkSystem
    {
        private readonly NetDataWriter _dataWriter;
        private readonly VoiceCraftWorld _world;
        private readonly EventBasedNetListener _listener;
        private readonly NetManager _netManager;
        private readonly ServerProperties _properties;

        public NetworkSystem(VoiceCraftServer server, NetManager netManager)
        {
            _dataWriter = new NetDataWriter();
            _world = server.World;
            _listener = server.Listener;
            _properties = server.Properties;
            _netManager = netManager;

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
            var status = true;
            foreach (var peer in peers)
            {
                status = SendPacket(peer, packet, deliveryMethod);
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
        
        public void SendEntityEffects(VoiceCraftEntity entity, VoiceCraftNetworkEntity targetEntity)
        {
            foreach (var effect in entity.Effects)
            {
                var setEffectPacket = new AddEffectPacket(entity.NetworkId, effect.Value);
                SendPacket(targetEntity.NetPeer, setEffectPacket);
            }
        }

        public void SendEntityData(VoiceCraftEntity entity, VoiceCraftNetworkEntity targetEntity)
        {
            var updatePositionPacket = new UpdatePositionPacket(entity.NetworkId, entity.Position);
            var updateRotationPacket = new UpdateRotationPacket(entity.NetworkId, entity.Rotation);

            SendPacket(targetEntity.NetPeer, updatePositionPacket);
            SendPacket(targetEntity.NetPeer, updateRotationPacket);
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
                    loginPeer.Tag = LoginType.Login;
                    _world.CreateEntity(loginPeer);
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
                        var infoPacket = new InfoPacket()
                        {
                            Clients = _netManager.ConnectedPeersCount,
                            Discovery = _properties.Discovery,
                            PositioningType = _properties.PositioningType,
                            Motd = _properties.Motd
                        };
                        SendUnconnectedPacket(remoteendpoint, infoPacket);
                        break;
                    //Unused
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
    }
}