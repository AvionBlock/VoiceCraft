using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Request
{
    public class McApiLogoutRequestPacket : IMcApiPacket
    {
        public McApiLogoutRequestPacket()
        {
        }

        public McApiPacketType PacketType => McApiPacketType.LogoutRequest;

        public void Serialize(NetDataWriter writer)
        {
        }

        public void Deserialize(NetDataReader reader)
        {
        }

        public McApiLogoutRequestPacket Set()
        {
            return this;
        }
    }
}