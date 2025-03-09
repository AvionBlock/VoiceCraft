using LiteNetLib;

namespace VoiceCraft.Core.Components
{
    public class NetworkComponent
    {
        public uint NetworkId { get; }
        public NetPeer Peer { get; }

        public NetworkComponent(uint networkId, NetPeer peer)
        {
            NetworkId = networkId;
            Peer = peer;
        }
    }
}