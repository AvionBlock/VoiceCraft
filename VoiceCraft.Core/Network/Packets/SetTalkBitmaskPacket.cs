using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetTalkBitmaskPacket : VoiceCraftPacket
    {
        public SetTalkBitmaskPacket(int id = 0, uint value = 0)
        {
            Id = id;
            Value = value;
        }

        public override PacketType PacketType => PacketType.SetTalkBitmask;

        public int Id { get; private set; }
        public uint Value { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Value);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Value = reader.GetUInt();
        }
    }
}