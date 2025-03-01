using LiteNetLib;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Client.Network.EventHandlers
{
    public class NetworkEventHandler
    {
        private readonly VoiceCraftClient _client;

        public NetworkEventHandler(VoiceCraftClient client)
        {
            _client = client;
            
            _client.Listener.PeerConnectedEvent += OnPeerConnected;
            _client.Listener.PeerDisconnectedEvent += OnPeerDisconnected;
            _client.Listener.NetworkLatencyUpdateEvent += OnNetworkLatencyUpdateEvent;
            _client.Listener.ConnectionRequestEvent += OnConnectionRequest;
        }
        
        private void OnPeerConnected(NetPeer peer)
        {
            _client.ConnectionStatus = ConnectionStatus.Connected;
            _client.ServerPeer = peer;
        }

        private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            _client.ConnectionStatus = ConnectionStatus.Disconnected;
            _client.ServerPeer = null;
        }
        
        private void OnNetworkLatencyUpdateEvent(NetPeer peer, int latency)
        {
            _client.Latency = latency;
        }

        private static void OnConnectionRequest(ConnectionRequest request)
        {
            request.Reject();
        }
    }
}