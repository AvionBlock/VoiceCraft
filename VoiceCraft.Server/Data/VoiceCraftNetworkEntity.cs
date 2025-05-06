using LiteNetLib;
using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Server.Data
{
    public class VoiceCraftNetworkEntity : VoiceCraftEntity
    {
        public NetPeer NetPeer { get; }
        public Guid UserGuid { get; private set; }
        public PositioningType PositioningType { get; }
        public override EntityType EntityType => EntityType.Network;

        public VoiceCraftNetworkEntity(NetPeer netPeer, Guid userGuid, PositioningType positioningType, VoiceCraftWorld world) : base(netPeer.Id, world)
        {
            NetPeer = netPeer;
            UserGuid = userGuid;
            PositioningType = positioningType;
            AddVisibleEntity(this); //Should always be visible to itself.
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(UserGuid);
            base.Serialize(writer);
        }

        public override void Deserialize(NetDataReader reader)
        {
            var userGuid = reader.GetGuid();
            base.Deserialize(reader);
            UserGuid = userGuid;
        }
    }
}