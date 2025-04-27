using LiteNetLib;
using VoiceCraft.Core;

namespace VoiceCraft.Server.Data
{
    public class VoiceCraftNetworkEntity : VoiceCraftEntity
    {
        public NetPeer NetPeer { get; }
        
        public VoiceCraftNetworkEntity(NetPeer netPeer, VoiceCraftWorld world) : base(netPeer.Id, world)
        {
            NetPeer = netPeer;
            AddVisibleEntity(this); //Should always be visible to itself.
        }
    }
}