using System;
using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Response;

public class VcDenyResponsePacket : IVoiceCraftPacket
{
    public VcDenyResponsePacket() : this(Guid.Empty, string.Empty)
    {
    }

    public VcDenyResponsePacket(Guid requestId, string reason)
    {
        RequestId = requestId;
        Reason = reason;
    }

    public Guid RequestId { get; private set; }

    public string Reason { get; private set; }

    public VcPacketType PacketType => VcPacketType.DenyResponse;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(RequestId);
        writer.Put(Reason, Constants.MaxStringLength);
    }

    public void Deserialize(NetDataReader reader)
    {
        RequestId = reader.GetGuid();
        Reason = reader.GetString(Constants.MaxStringLength);
    }

    public VcDenyResponsePacket Set(Guid requestId = new(), string reason = "")
    {
        RequestId = requestId;
        Reason = reason;
        return this;
    }
}