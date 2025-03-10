using Arch.Core;
using LiteNetLib;
using VoiceCraft.Core;
using VoiceCraft.Core.Components;
using VoiceCraft.Core.Events;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Server.EventHandlers
{
    public class NetworkEventHandler
    {
        private readonly VoiceCraftServer _server;
        private readonly EventBasedNetListener _listener;
        private readonly World _world;
        private readonly ServerProperties _properties;
        
        public NetworkEventHandler(VoiceCraftServer server)
        {
            _server = server;
            _listener = _server.Listener;
            _world = _server.World;
            _properties = _server.Properties;
            
            _listener.ConnectionRequestEvent += OnConnectionRequest;
            _listener.PeerConnectedEvent += OnPeerConnected;
            _listener.PeerDisconnectedEvent += OnPeerDisconnected; 
            _listener.NetworkReceiveEvent += OnNetworkReceiveEvent;
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
                        loginPeer.Tag = loginPacket.LoginType;
                        var entity = _world.Create();
                        WorldEventHandler.InvokeEntityCreated(new EntityCreatedEvent(entity));
                        _ = new NetworkComponent(entity, IdGenerator.Generate(), loginPeer);
                        break;
                    case LoginType.Pinger:
                    case LoginType.Discovery:
                        var peer = request.Accept();
                        peer.Tag = loginPacket.LoginType;
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

        private void OnPeerConnected(NetPeer peer)
        {
            if ((LoginType?)peer.Tag != LoginType.Pinger) return;
            var serverInfoPacket = new InfoPacket()
            {
                Motd = _properties.Motd,
                Discovery = _properties.Discovery,
                PositioningType = _properties.PositioningType,
            };
            _server.SendPacket(peer, serverInfoPacket);
        }

        private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectinfo)
        {
            var query = new QueryDescription()
                .WithAll<NetworkComponent>();
            _world.Query(in query, entity =>
            {
                var networkComponent = _world.Get<NetworkComponent>(entity);
                if(networkComponent.Peer.Equals(peer))
                    _world.Destroy(entity);
            });
        }

        private void OnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliverymethod)
        {
            var packetType = reader.GetByte();
            var pt = (PacketType)packetType;
            switch (pt)
            {
                case PacketType.Login:
                case PacketType.Info:
                case PacketType.Audio:
                case PacketType.SetLocalEntity:
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