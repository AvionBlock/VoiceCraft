using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets
{
    public class McApiLoginPacket : McApiPacket
    {
        public McApiLoginPacket(string loginToken = "", Version? version = null)
        {
            LoginToken = loginToken;
            Version = version ?? new Version(0, 0, 0);
        }

        public override McApiPacketType PacketType => McApiPacketType.Login;
        public string LoginToken { get; private set; }
        public Version Version { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(LoginToken, Constants.MaxStringLength);
            writer.Put(Version.Major);
            writer.Put(Version.Minor);
            writer.Put(Version.Build);
        }

        public override void Deserialize(NetDataReader reader)
        {
            LoginToken = reader.GetString(Constants.MaxStringLength);
            Version = new Version(reader.GetInt(), reader.GetInt(), reader.GetInt());
        }
    }
}