using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class EntityRemovedPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.EntityRemoved;
        public int NetworkId { get; set; }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(NetworkId);
        }

        public override void Deserialize(NetDataReader reader)
        {
            NetworkId = reader.GetInt();
        }
    }
}