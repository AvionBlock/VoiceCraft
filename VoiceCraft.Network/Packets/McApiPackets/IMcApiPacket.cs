using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.McApiPackets;

public interface IMcApiPacket : INetSerializable
{
    McApiPacketType PacketType { get; }
}