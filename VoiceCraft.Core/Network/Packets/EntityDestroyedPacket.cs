using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class EntityDestroyedPacket : VoiceCraftPacket
    {
        public EntityDestroyedPacket(int id = 0)
        {
            Id = id;
        }

        public override PacketType PacketType => PacketType.EntityDestroyed;

        public int Id { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
        }
    }
}