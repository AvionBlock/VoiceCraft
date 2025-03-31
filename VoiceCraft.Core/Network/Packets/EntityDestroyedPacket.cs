using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class EntityDestroyedPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.EntityDestroyed;
        public int Id { get; private set; }

        public EntityDestroyedPacket(int id = 0)
        {
            Id = id;
        }
        
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