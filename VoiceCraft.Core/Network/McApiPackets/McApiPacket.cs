using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets
{
    public abstract class McApiPacket : INetSerializable
    {
        public abstract McApiPacketType PacketType { get; }

        public abstract void Serialize(NetDataWriter writer);

        public abstract void Deserialize(NetDataReader reader);
    }
}