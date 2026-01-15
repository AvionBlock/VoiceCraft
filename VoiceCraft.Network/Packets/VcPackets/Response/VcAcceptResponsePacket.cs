using System;
using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Response;

public class VcAcceptResponsePacket : IVoiceCraftPacket, IVoiceCraftRIdPacket
{
    public VcAcceptResponsePacket() : this(Guid.Empty)
    {
    }

    public VcAcceptResponsePacket(Guid requestId)
    {
        RequestId = requestId;
    }

    public VcPacketType PacketType => VcPacketType.AcceptResponse;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(RequestId);
    }

    public void Deserialize(NetDataReader reader)
    {
        RequestId = reader.GetGuid();
    }

    public Guid RequestId { get; private set; }

    public VcAcceptResponsePacket Set(Guid requestId = new())
    {
        RequestId = requestId;
        return this;
    }
}