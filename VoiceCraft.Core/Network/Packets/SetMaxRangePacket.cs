using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetMaxRangePacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetMaxRange;
        public byte Id { get; private set; }
        public int MaxRange { get; private set; }

        public SetMaxRangePacket(byte id = 0, int maxRange = 0)
        {
            Id = id;
            MaxRange = maxRange;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(MaxRange);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetByte();
            MaxRange = reader.GetInt();
        }
    }
}