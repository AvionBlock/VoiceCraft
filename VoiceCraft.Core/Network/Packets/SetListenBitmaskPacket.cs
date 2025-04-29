using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetListenBitmaskPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetListenBitmask;
        
        public int Id { get; private set; }
        public ulong Value { get; private set; }

        public SetListenBitmaskPacket(int id = 0, ulong value = 0)
        {
            Id = id;
            Value = value;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Value);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Value = reader.GetULong();
        }
    }
}