using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets
{
    public class McApiLogoutPacket : McApiPacket
    {
        public McApiLogoutPacket(string token = "")
        {
            Token = token;
        }

        public override McApiPacketType PacketType => McApiPacketType.Logout;
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