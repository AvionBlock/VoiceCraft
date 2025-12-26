using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Request
{
    public class McApiClearEffectsRequestPacket : IMcApiPacket
    {
        public McApiPacketType PacketType => McApiPacketType.ClearEffectsRequest;

        public void Serialize(NetDataWriter writer)
        {
        }

        public void Deserialize(NetDataReader reader)
        {
        }

        public McApiClearEffectsRequestPacket Set()
        {
            return this;
        }
    }
}