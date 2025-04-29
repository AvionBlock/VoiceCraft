using LiteNetLib;
using VoiceCraft.Core;

namespace VoiceCraft.Server.Data
{
    public class VoiceCraftNetworkEntity : VoiceCraftEntity
    {
        public NetPeer NetPeer { get; }
        public Guid UserGuid { get; }
        public PositioningType PositioningType { get; }
        
        public VoiceCraftNetworkEntity(NetPeer netPeer, Guid userGuid, PositioningType positioningType, VoiceCraftWorld world) : base(netPeer.Id, world)
        {
            NetPeer = netPeer;
            UserGuid = userGuid;
            PositioningType = positioningType;
            AddVisibleEntity(this); //Should always be visible to itself.
        }
    }
}