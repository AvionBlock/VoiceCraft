using LiteNetLib.Utils;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Network.Packets.McApiPackets;

public interface IMcApiPacket : INetSerializable, IPooledPacket
{
    McApiPacketType PacketType { get; }
}