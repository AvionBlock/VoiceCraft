using LiteNetLib;

namespace VoiceCraft.Core.Components
{
    public struct NetworkComponent
    {
        public readonly uint NetworkId;
        public readonly NetPeer? Peer;

        public NetworkComponent(uint networkId, NetPeer? peer = null)
        {
            NetworkId = networkId;
            Peer = peer;
        }
    }
}