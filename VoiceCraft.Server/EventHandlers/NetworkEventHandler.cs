using Friflo.Engine.ECS;
using LiteNetLib;
using VoiceCraft.Core;
using VoiceCraft.Core.Components;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Server.EventHandlers
{
    public class NetworkEventHandler
    {
        private readonly VoiceCraftServer _server;
        
        public NetworkEventHandler(VoiceCraftServer server)
        {
            _server = server;
            
            _server.Listener.ConnectionRequestEvent += OnConnectionRequest;
            _server.Listener.PeerConnectedEvent += OnPeerConnected;
            _server.Listener.PeerDisconnectedEvent += OnPeerDisconnected;
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
                        _server.World.CreateEntity(new NetworkComponent(IdGenerator.Generate(), loginPeer));
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
                Motd = _server.Motd,
                Discovery = _server.DiscoveryEnabled,
                PositioningType = _server.PositioningType,
            };
            _server.SendPacket(peer, serverInfoPacket);
        }

        private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectinfo)
        {
            var query = _server.World.Query<NetworkComponent>();
            var buffer = _server.World.GetCommandBuffer();
            query.ForEachEntity((ref NetworkComponent c1, Entity entity) =>
            {
                if (c1.Peer?.Equals(peer) ?? false)
                    buffer.DeleteEntity(entity.Id);
            });
            buffer.Playback();
        }
    }
}