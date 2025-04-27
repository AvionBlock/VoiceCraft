using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetMaxRangePacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetMaxRange;
        public int MaxRange { get; private set; }

        public SetMaxRangePacket(int maxRange = 0)
        {
            MaxRange = maxRange;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(MaxRange);
        }

        public override void Deserialize(NetDataReader reader)
        {
            MaxRange = reader.GetInt();
        }
    }
}