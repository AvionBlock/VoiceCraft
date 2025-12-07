using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Request
{
    public class VcSetEntityVisibilityRequestPacket : IVoiceCraftPacket
    {
        public VcSetEntityVisibilityRequestPacket(int id = 0, bool value = false)
        {
            Id = id;
            Value = value;
        }

        public VcPacketType PacketType => VcPacketType.SetEntityVisibilityRequest;

        public int Id { get; private set; }
        public bool Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Value);
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Value = reader.GetBool();
        }

        public VcSetEntityVisibilityRequestPacket Set(int id = 0, bool value = false)
        {
            Id = id;
            Value = value;
            return this;
        }
    }
}