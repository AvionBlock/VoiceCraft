using LiteNetLib;
using VoiceCraft.Core.Data;

namespace VoiceCraft.Core.Network
{
    public class NetworkEntity : VoiceCraftEntity
    {
        public readonly NetPeer Peer;
        
        public NetworkEntity(NetPeer peer)
        {
            Peer = peer;
        }
    }
}