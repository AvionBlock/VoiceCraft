using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Request
{
    public class McApiLoginRequestPacket : IMcApiPacket, IMcApiRIdPacket
    {
        public McApiLoginRequestPacket() : this(string.Empty, string.Empty, new Version(0, 0, 0))
        {
        }

        public McApiLoginRequestPacket(string requestId, string token, Version version)
        {
            RequestId = requestId;
            Token = token;
            Version = version;
        }

        public McApiPacketType PacketType => McApiPacketType.LoginRequest;
        public string RequestId { get; private set; }
        public string Token { get; private set; }
        public Version Version { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(RequestId, Constants.MaxStringLength);
            writer.Put(Token, Constants.MaxStringLength);
            writer.Put(Version.Major);
            writer.Put(Version.Minor);
            writer.Put(Version.Build);
        }

        public void Deserialize(NetDataReader reader)
        {
            RequestId = reader.GetString(Constants.MaxStringLength);
            Token = reader.GetString(Constants.MaxStringLength);
            Version = new Version(reader.GetInt(), reader.GetInt(), reader.GetInt());
        }

        public McApiLoginRequestPacket Set(string requestId = "", string token = "", Version? version = null)
        {
            RequestId = requestId;
            Token = token;
            Version = version ?? new Version(0, 0, 0);
            return this;
        }
    }
}