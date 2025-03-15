using LiteNetLib;

namespace VoiceCraft.Core
{
    public class VoiceCraftClientEntity : VoiceCraftEntity
    {
        public NetPeer NetPeer { get; }
        
        public VoiceCraftClientEntity(NetPeer netPeer) : base(netPeer.Id)
        {
            NetPeer = netPeer;
        }
    }
}