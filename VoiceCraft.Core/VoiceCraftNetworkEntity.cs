using LiteNetLib;

namespace VoiceCraft.Core
{
    public class VoiceCraftNetworkEntity : VoiceCraftEntity
    {
        public NetPeer NetPeer { get; }
        
        public VoiceCraftNetworkEntity(NetPeer netPeer) : base((short)netPeer.Id)
        {
            NetPeer = netPeer;
            VisibleEntities.Add(this); //Should always be visible to itself.
        }
    }
}