using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Event
{
    public class VcOnEntityMuffleFactorUpdatedPacket : IVoiceCraftPacket
    {
        public VcOnEntityMuffleFactorUpdatedPacket(int id = 0, float value = 0f)
        {
            Id = id;
            Value = value;
        }

        public VcPacketType PacketType => VcPacketType.OnEntityMuffleFactorUpdated;

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
        
        public VcOnEntityMuffleFactorUpdatedPacket Set(int id = 0, float value = 0f)
        {
            Id = id;
            Value = value;
            return this;
        }
    }
}