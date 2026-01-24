using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Response;

public class McApiPingResponsePacket(string token = "") : IMcApiPacket
{
    public McApiPingResponsePacket() : this(string.Empty)
    {
    }

    public string Token { get; private set; } = token;

    public McApiPacketType PacketType => McApiPacketType.PingResponse;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Token, Constants.MaxStringLength);
    }

    public void Deserialize(NetDataReader reader)
    {
        Token = reader.GetString(Constants.MaxStringLength);
    }

    public McApiPingResponsePacket Set(string token = "")
    {
        Token = token;
        return this;
    }
}