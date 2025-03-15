using System.Net;
using LiteNetLib;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Server.EventHandlers
{
    public class NetworkEventHandler
    {
        private readonly VoiceCraftServer _server;
        private readonly EventBasedNetListener _listener;
        private readonly NetManager _netManager;
        private readonly ServerProperties _properties;

        public NetworkEventHandler(VoiceCraftServer server, NetManager netManager)
        {
            _server = server;
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

                switch (loginPacket.LoginType)
                {
                    case LoginType.Login:
                        var loginPeer = request.Accept();
                        loginPeer.Tag = LoginType.Login;
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
            catch
            {
                request.Reject(); //Need to set message data here.
            }
        }

        private void OnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliverymethod)
        {
            var packetType = reader.GetByte();
            var pt = (PacketType)packetType;
            switch (pt)
            {
                case PacketType.Login:
                case PacketType.Info:
                case PacketType.EntityCreated:
                case PacketType.EntityDestroyed:
                case PacketType.AddComponent:
                case PacketType.RemoveComponent:
                case PacketType.UpdateComponent:
                default:
                    break;
            }

            reader.Recycle();
        }

        private void OnNetworkReceiveUnconnectedEvent(IPEndPoint remoteendpoint, NetPacketReader reader, UnconnectedMessageType messagetype)
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
                case PacketType.AddComponent:
                case PacketType.RemoveComponent:
                case PacketType.UpdateComponent:
                default:
                    break;
            }

            reader.Recycle();
        }
    }
}