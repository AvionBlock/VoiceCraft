using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Response
{
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

        public Guid RequestId { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(RequestId);
        }

        public void Deserialize(NetDataReader reader)
        {
            RequestId = reader.GetGuid();
        }

        public VcAcceptResponsePacket Set(Guid requestId = new Guid())
        {
            RequestId = requestId;
            return this;
        }
    }
}