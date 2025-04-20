using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetMinRangePacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetMinRange;
        public byte Id { get; private set; }
        public int MinRange { get; private set; }

        public SetMinRangePacket(byte id = 0, int minRange = 0)
        {
            Id = id;
            MinRange = minRange;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(MinRange);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetByte();
            MinRange = reader.GetInt();
        }
    }
}