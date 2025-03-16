using LiteNetLib;

namespace VoiceCraft.Core
{
    public class VoiceCraftNetworkEntity : VoiceCraftEntity
    {
        public NetPeer NetPeer { get; }
        
        public VoiceCraftNetworkEntity(NetPeer netPeer) : base(netPeer.Id)
        {
            NetPeer = netPeer;
        }
    }
}