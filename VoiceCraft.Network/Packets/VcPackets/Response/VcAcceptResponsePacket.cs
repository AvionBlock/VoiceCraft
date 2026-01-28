using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets.Response;

public class VcAcceptResponsePacket(Guid requestId) : IVoiceCraftPacket, IVoiceCraftRIdPacket
{
    public VcAcceptResponsePacket() : this(Guid.Empty)
    {
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

    public Guid RequestId { get; private set; } = requestId;

    public VcAcceptResponsePacket Set(Guid requestId = new())
    {
        RequestId = requestId;
        return this;
    }
}