using System;
using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Request;

public class McApiLoginRequestPacket(string requestId, string token, Version version) : IMcApiPacket, IMcApiRIdPacket
{
    public McApiLoginRequestPacket() : this(string.Empty, string.Empty, new Version(0, 0, 0))
    {
    }

    public string Token { get; private set; } = token;
    public Version Version { get; private set; } = version;

    public McApiPacketType PacketType => McApiPacketType.LoginRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(RequestId, Constants.MaxStringLength);
        writer.Put(Token, Constants.MaxStringLength);
        writer.Put(Version.Major);
        writer.Put(Version.Minor);
        writer.Put(Version.Build);
    }

    public void Deserialize(NetDataReader reader)
    {
        RequestId = reader.GetString(Constants.MaxStringLength);
        Token = reader.GetString(Constants.MaxStringLength);
        Version = new Version(reader.GetInt(), reader.GetInt(), reader.GetInt());
    }

    public string RequestId { get; private set; } = requestId;

    public McApiLoginRequestPacket Set(string requestId = "", string token = "", Version? version = null)
    {
        RequestId = requestId;
        Token = token;
        Version = version ?? new Version(0, 0, 0);
        return this;
    }
}