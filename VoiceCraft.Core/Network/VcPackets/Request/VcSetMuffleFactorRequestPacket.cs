using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Request
{
    public class VcSetMuffleFactorRequest : IVoiceCraftPacket
    {
        public VcSetMuffleFactorRequest() : this(0.0f)
        {
        }

        public VcSetMuffleFactorRequest(float value)
        {
            Value = value;
        }

        public VcPacketType PacketType => VcPacketType.SetMuffleFactorRequest;
        
        public float Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Value);
        }

        public void Deserialize(NetDataReader reader)
        {
            Value = reader.GetUShort();
        }

        public VcSetMuffleFactorRequest Set(float value = 0.0f)
        {
            Value = value;
            return this;
        }
    }
}