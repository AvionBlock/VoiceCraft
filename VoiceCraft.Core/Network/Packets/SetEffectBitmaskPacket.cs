using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetEffectBitmaskPacket : VoiceCraftPacket
    {
        public SetEffectBitmaskPacket(int id = 0, ushort value = 0)
        {
            Id = id;
            Value = value;
        }

        public override PacketType PacketType => PacketType.SetEffectBitmask;

        public int Id { get; private set; }
        public ushort Value { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Value);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Value = reader.GetUShort();
        }
    }
}