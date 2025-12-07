using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Request
{
    public class VcSetMuteRequestPacket : IVoiceCraftPacket
    {
        public VcSetMuteRequestPacket(bool value = true)
        {
            Value = value;
        }

        public VcPacketType PacketType => VcPacketType.SetMuteRequest;
        
        public bool Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Value);
        }

        public void Deserialize(NetDataReader reader)
        {
            Value = reader.GetBool();
        }
        
        public VcSetMuteRequestPacket Set(bool value = true)
        {
            Value = value;
            return this;
        }
    }
}