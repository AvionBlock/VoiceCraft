using LiteNetLib.Utils;

namespace VoiceCraft.Core.Data.Packets
{
    public abstract class VoiceCraftPacket : INetSerializable
    {
        public abstract PacketType PacketType { get; }
        public abstract void Serialize(NetDataWriter writer);
        public abstract void Deserialize(NetDataReader reader);
    }
}