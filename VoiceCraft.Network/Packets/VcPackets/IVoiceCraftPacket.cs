using LiteNetLib.Utils;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Network.Packets.VcPackets;

public interface IVoiceCraftPacket : INetSerializable, IPooledPacket
{
    public VcPacketType PacketType { get; }
}