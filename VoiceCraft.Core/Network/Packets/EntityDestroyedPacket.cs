using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class EntityDestroyedPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.EntityDestroyed;
        public int Id { get; set; }
        public int WorldId { get; set; }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(WorldId);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            WorldId = reader.GetInt();
        }
    }
}