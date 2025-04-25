using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetTalkBitmaskPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetTalkBitmask;
        public int Id { get; private set; }
        public ulong Bitmask { get; private set; }

        public SetTalkBitmaskPacket(int id = 0, ulong bitmask = 0)
        {
            Id = id;
            Bitmask = bitmask;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Bitmask);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Bitmask = reader.GetULong();
        }
    }
}