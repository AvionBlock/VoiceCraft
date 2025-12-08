using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Request
{
    public class VcSetCaveFactorRequest : IVoiceCraftPacket
    {
        public VcSetCaveFactorRequest() : this(0.0f)
        {
        }

        public VcSetCaveFactorRequest(float value)
        {
            Value = value;
        }

        public VcPacketType PacketType => VcPacketType.SetCaveFactorRequest;
        
        public float Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Value);
        }

        public void Deserialize(NetDataReader reader)
        {
            Value = reader.GetUShort();
        }

        public VcSetCaveFactorRequest Set(float value = 0.0f)
        {
            Value = value;
            return this;
        }
    }
}