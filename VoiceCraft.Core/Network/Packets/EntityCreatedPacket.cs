using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class EntityCreatedPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.EntityCreated;
        public int Id { get; private set; }
        public VoiceCraftEntity Entity { get; private set; }

        public EntityCreatedPacket(int id = 0, VoiceCraftEntity? entity = null)
        {
            Id = id;
            Entity = entity ?? new VoiceCraftEntity(id);
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Entity.Id);
            writer.Put(Entity);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Entity = new VoiceCraftEntity(Id);
            Entity.Deserialize(reader);
        }
    }
}