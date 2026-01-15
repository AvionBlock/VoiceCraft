using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets;

public interface IMcApiPacket : INetSerializable
{
    McApiPacketType PacketType { get; }
}