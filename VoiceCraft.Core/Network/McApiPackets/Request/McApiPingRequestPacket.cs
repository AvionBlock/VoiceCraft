using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Request
{
    public class McApiPingRequestPacket : McApiPacket
    {
        public McApiPingRequestPacket(string token = "")
        {
            Token = token;
        }

        public override McApiPacketType PacketType => McApiPacketType.PingRequest;
        public string Token { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Token, Constants.MaxStringLength);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Token = reader.GetString(Constants.MaxStringLength);
        }
    }
}