using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetEffectBitmaskPacket : VoiceCraftPacket
    {
        public SetEffectBitmaskPacket(int id = 0, uint value = 0)
        {
            Id = id;
            Value = value;
        }

        public override PacketType PacketType => PacketType.SetEffectBitmask;

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