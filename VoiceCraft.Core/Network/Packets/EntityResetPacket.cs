using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class EntityResetPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.EntityReset;
        public byte Id { get; private set; }

        public EntityResetPacket(byte id = 0)
        {
            Id = id;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetByte();
        }
    }
}