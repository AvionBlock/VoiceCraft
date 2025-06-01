using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McLinkPackets
{
    public class LoginPacket : McLinkPacket
    {
        public LoginPacket(Guid loginKey = new Guid())
        {
            LoginKey = loginKey;
        }

        public override McLinkPacketType PacketType => McLinkPacketType.Login;
        public Guid LoginKey { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(LoginKey);
        }

        public override void Deserialize(NetDataReader reader)
        {
            LoginKey = reader.GetGuid();
        }
    }
}