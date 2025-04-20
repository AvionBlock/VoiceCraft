using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class EntityCreatedPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.EntityCreated;
        public byte Id { get; private set; }
        public VoiceCraftEntity Entity { get; private set; }

        public EntityCreatedPacket(VoiceCraftEntity? entity = null)
        {
            Entity = entity ?? new VoiceCraftEntity(0);
            Id = Entity.Id;
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Entity.Id);
            writer.Put(Entity);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetByte();
            Entity = new VoiceCraftEntity(Id);
            Entity.Deserialize(reader);
        }
    }
}