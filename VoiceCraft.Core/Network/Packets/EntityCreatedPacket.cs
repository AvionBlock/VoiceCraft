using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class EntityCreatedPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.EntityCreated;
        public int NetworkId { get; set; }
        public VoiceCraftEntity? Entity { get; set; }

        public EntityCreatedPacket(VoiceCraftEntity? entity = null)
        {
            NetworkId = entity?.Id ?? 0;
            Entity = entity;
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Entity?.Id ?? 0);
            writer.Put(Entity);
        }

        public override void Deserialize(NetDataReader reader)
        {
            NetworkId = reader.GetInt();
        }
    }
}