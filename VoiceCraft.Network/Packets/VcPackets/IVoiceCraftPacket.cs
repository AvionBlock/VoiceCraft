using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets;

public interface IVoiceCraftPacket : INetSerializable
{
    public VcPacketType PacketType { get; }
}