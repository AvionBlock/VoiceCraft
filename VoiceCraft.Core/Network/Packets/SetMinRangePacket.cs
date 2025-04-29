using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetMinRangePacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetMinRange;
        
        public int Value { get; private set; }

        public SetMinRangePacket(int value = 0)
        {
            Value = value;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Value);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Value = reader.GetInt();
        }
    }
}