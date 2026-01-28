using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets;

public interface IVoiceCraftPacket : INetSerializable
{
    public VcPacketType PacketType { get; }
}