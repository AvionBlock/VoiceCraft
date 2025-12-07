using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Response
{
    public class VcDenyResponsePacket : IVoiceCraftPacket
    {
        public VcDenyResponsePacket(Guid requestId = new Guid())
        {
            RequestId = requestId;
        }

        public VcPacketType PacketType => VcPacketType.DenyResponse;
        
        public Guid RequestId { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(RequestId);
        }

        public void Deserialize(NetDataReader reader)
        {
            RequestId = reader.GetGuid();
        }
        
        public VcDenyResponsePacket Set(Guid requestId = new Guid())
        {
            RequestId = requestId;
            return this;
        }
    }
}