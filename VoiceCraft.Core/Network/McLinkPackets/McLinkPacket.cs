using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McLinkPackets
{
    public abstract class McLinkPacket : INetSerializable
    {
        public abstract McLinkPacketType PacketType { get; }

        public abstract void Serialize(NetDataWriter writer);

        public abstract void Deserialize(NetDataReader reader);
    }
}