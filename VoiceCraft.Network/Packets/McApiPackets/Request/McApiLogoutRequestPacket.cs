using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Request;

public class McApiLogoutRequestPacket : IMcApiPacket
{
    public McApiLogoutRequestPacket() : this(string.Empty)
    {
    }

    public McApiLogoutRequestPacket(string token = "")
    {
        Token = token;
    }

    public string Token { get; private set; }

    public McApiPacketType PacketType => McApiPacketType.LogoutRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Token, Constants.MaxStringLength);
    }

    public void Deserialize(NetDataReader reader)
    {
        Token = reader.GetString(Constants.MaxStringLength);
    }

    public McApiLogoutRequestPacket Set(string token = "")
    {
        Token = token;
        return this;
    }
}