using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets
{
    public interface IMcApiPacket : INetSerializable
    {
        McApiPacketType PacketType { get; }
    }
}