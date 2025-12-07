using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets
{
    public class McApiLoginRequestPacket : McApiPacket
    {
        public McApiLoginRequestPacket(string requestId = "", string token = "", Version? version = null)
        {
            RequestId = requestId;
            Token = token;
            Version = version ?? new Version(0, 0, 0);
        }

        public override McApiPacketType PacketType => McApiPacketType.LoginRequest;
        public string RequestId { get; private set; }
        public string Token { get; private set; }
        public Version Version { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(RequestId, Constants.MaxStringLength);
            writer.Put(Token, Constants.MaxStringLength);
            writer.Put(Version.Major);
            writer.Put(Version.Minor);
            writer.Put(Version.Build);
        }

        public override void Deserialize(NetDataReader reader)
        {
            RequestId = reader.GetString(Constants.MaxStringLength);
            Token = reader.GetString(Constants.MaxStringLength);
            Version = new Version(reader.GetInt(), reader.GetInt(), reader.GetInt());
        }
    }
}