using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetMinRangePacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetMinRange;
        public int MinRange { get; private set; }

        public SetMinRangePacket(int minRange = 0)
        {
            MinRange = minRange;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(MinRange);
        }

        public override void Deserialize(NetDataReader reader)
        {
            MinRange = reader.GetInt();
        }
    }
}