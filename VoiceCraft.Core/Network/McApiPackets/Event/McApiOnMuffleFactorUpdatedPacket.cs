using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Event
{
    public class McApiOnMuffleFactorUpdatedPacket : IMcApiPacket
    {
        public McApiOnMuffleFactorUpdatedPacket(int id = 0, float value = 0.0f)
        {
            Id = id;
            Value = value;
        }

        public McApiPacketType PacketType => McApiPacketType.OnEntityMuffleFactorUpdated;

        public int Id { get; private set; }
        public float Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Value);
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Value = reader.GetFloat();
        }
        
        public void Set(int id = 0, float value = 0.0f)
        {
            Id = id;
            Value = value;
        }
    }
}