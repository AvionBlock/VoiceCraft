using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets
{
    public interface IVoiceCraftPacket : INetSerializable
    {
        public VcPacketType PacketType { get; }
    }
}