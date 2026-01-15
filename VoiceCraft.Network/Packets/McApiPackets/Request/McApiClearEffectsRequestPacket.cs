using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Request;

public class McApiClearEffectsRequestPacket : IMcApiPacket
{
    public McApiPacketType PacketType => McApiPacketType.ClearEffectsRequest;

    public void Serialize(NetDataWriter writer)
    {
    }

    public void Deserialize(NetDataReader reader)
    {
    }

    public McApiClearEffectsRequestPacket Set()
    {
        return this;
    }
}