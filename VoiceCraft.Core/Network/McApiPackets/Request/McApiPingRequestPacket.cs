using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Request
{
    public class McApiPingRequestPacket : IMcApiPacket
    {
        public McApiPacketType PacketType => McApiPacketType.PingRequest;

        public void Serialize(NetDataWriter writer)
        {
        }

        public void Deserialize(NetDataReader reader)
        {
        }

        public McApiPingRequestPacket Set()
        {
            return this;
        }
    }
}