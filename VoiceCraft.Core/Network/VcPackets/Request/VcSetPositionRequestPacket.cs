using System.Numerics;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Request
{
    public class VcSetPositionRequestPacket : IVoiceCraftPacket
    {
        public VcSetPositionRequestPacket() : this(Vector3.Zero)
        {
        }

        public VcSetPositionRequestPacket(Vector3 value)
        {
            Value = value;
        }

        public VcPacketType PacketType => VcPacketType.SetPositionRequest;
        
        public Vector3 Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Value.X);
            writer.Put(Value.Y);
            writer.Put(Value.Z);
        }

        public void Deserialize(NetDataReader reader)
        {
            Value = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
        }

        public VcSetPositionRequestPacket Set(Vector3 value = new Vector3())
        {
            Value = value;
            return this;
        }
    }
}