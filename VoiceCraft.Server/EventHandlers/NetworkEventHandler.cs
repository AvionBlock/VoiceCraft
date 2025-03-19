using System.Diagnostics;
using System.Net;
using LiteNetLib;
using VoiceCraft.Core;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Server.EventHandlers
{
    public class NetworkEventHandler
    {
        private readonly VoiceCraftServer _server;
        private readonly VoiceCraftWorld _world;
        private readonly EventBasedNetListener _listener;
        private readonly NetManager _netManager;
        private readonly ServerProperties _properties;

        public NetworkEventHandler(VoiceCraftServer server, NetManager netManager)
        {
            _server = server;
            _world = _server.World;
            _listener = _server.Listener;
            _properties = _server.Properties;
            _netManager = netManager;

            _listener.ConnectionRequestEvent += OnConnectionRequest;
            _listener.NetworkReceiveEvent += OnNetworkReceiveEvent;
            _listener.NetworkReceiveUnconnectedEvent += OnNetworkReceiveUnconnectedEvent;
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
                    request.Reject();
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
                    case PacketType.SetEffect:
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
                        _server.SendUnconnectedPacket(remoteendpoint, infoPacket);
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
                    case PacketType.SetEffect:
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