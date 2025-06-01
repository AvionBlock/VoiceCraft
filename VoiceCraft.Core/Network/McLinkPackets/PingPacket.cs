using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McLinkPackets
{
    public class PingPacket : McLinkPacket
    {
        public PingPacket(Guid token = new Guid())
        {
            Token = token;
        }

        public override McLinkPacketType PacketType => McLinkPacketType.Ping;
        public Guid Token { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Token);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Token = reader.GetGuid();
        }
    }
}