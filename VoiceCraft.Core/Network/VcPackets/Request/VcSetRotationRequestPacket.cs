using System.Numerics;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Request
{
    public class VcSetRotationRequestPacket : IVoiceCraftPacket
    {
        public VcSetRotationRequestPacket() : this(Vector2.Zero)
        {
        }

        public VcSetRotationRequestPacket(Vector2 value)
        {
            Value = value;
        }

        public VcPacketType PacketType => VcPacketType.SetRotationRequest;
        
        public Vector2 Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Value.X);
            writer.Put(Value.Y);
        }

        public void Deserialize(NetDataReader reader)
        {
            Value = new Vector2(reader.GetFloat(), reader.GetFloat());
        }

        public VcSetRotationRequestPacket Set(Vector2 value = new Vector2())
        {
            Value = value;
            return this;
        }
    }
}