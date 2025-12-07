using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets
{
    public class McApiOnEntityCaveFactorUpdatedPacket : McApiPacket
    {
        public McApiOnEntityCaveFactorUpdatedPacket(int id = 0, float value = 0.0f)
        {
            Id = id;
            Value = value;
        }

        public override McApiPacketType PacketType => McApiPacketType.OnEntityCaveFactorUpdated;

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