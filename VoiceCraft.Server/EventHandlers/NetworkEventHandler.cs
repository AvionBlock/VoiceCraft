using Arch.Core;
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
                        var entity = _server.World.Create();
                        _server.World.Add(entity, new NetworkComponent(IdGenerator.Generate(), loginPeer));
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
            var query = new QueryDescription()
                .WithAll<NetworkComponent>();
            _server.World.Query(in query, entity =>
            {
                var networkComponent = _server.World.Get<NetworkComponent>(entity);
                if(networkComponent.Peer?.Equals(peer) ?? false)
                    _server.World.Destroy(entity);
            });
        }
    }
}