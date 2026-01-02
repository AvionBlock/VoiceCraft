using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Request
{
    public class McApiResetRequestPacket : IMcApiPacket, IMcApiRIdPacket
    {
        public McApiResetRequestPacket() : this(string.Empty)
        {
        }

        public McApiResetRequestPacket(string requestId)
        {
            RequestId = requestId;
        }
        public McApiPacketType PacketType => McApiPacketType.ResetRequest;

        public string RequestId { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(RequestId, Constants.MaxStringLength);
        }

        public void Deserialize(NetDataReader reader)
        {
            RequestId = reader.GetString(Constants.MaxStringLength);
        }

        public void Set(string requestId = "")
        {
            RequestId = requestId;
        }
    }
}