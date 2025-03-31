using LiteNetLib;

namespace VoiceCraft.Core
{
    public class VoiceCraftNetworkEntity : VoiceCraftEntity
    {
        public NetPeer NetPeer { get; }
        
        public VoiceCraftNetworkEntity(NetPeer netPeer) : base((short)netPeer.Id)
        {
            NetPeer = netPeer;
        }
    }
}