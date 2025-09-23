using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetMuffleFactorPacket : VoiceCraftPacket
    {
        public SetMuffleFactorPacket(int id = 0, float value = 0f)
        {
            Id = id;
            Value = value;
        }

        public override PacketType PacketType => PacketType.SetMuffleFactor;

        public int Id { get; private set; }
        public float Value { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Value);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Value = reader.GetFloat();
        }
    }
}