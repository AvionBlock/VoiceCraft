using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Response
{
    public class VcAcceptResponsePacket : IVoiceCraftPacket
    {
        public VcAcceptResponsePacket(Guid requestId = new Guid(), IVoiceCraftPacket? data = null)
        {
            RequestId = requestId;
            Data = data;
        }

        public VcPacketType PacketType => VcPacketType.AcceptResponse;

        public Guid RequestId { get; private set; }
        public IVoiceCraftPacket? Data { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(RequestId);
            Data?.Serialize(writer);
        }

        public void Deserialize(NetDataReader reader)
        {
            RequestId = reader.GetGuid();
        }
        
        public VcAcceptResponsePacket Set(Guid requestId = new Guid(), IVoiceCraftPacket? data = null)
        {
            RequestId = requestId;
            Data = data;
            return this;
        }
    }

    public class VcSetIdAcceptResponsePacket : IVoiceCraftPacket
    {
        public VcSetIdAcceptResponsePacket(int id = 0)
        {
            Id = id;
        }
        
        public int Id { get; private set; }
        
        public VcPacketType PacketType => VcPacketType.SetIdAcceptResponse;
        
        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
        }

        public VcSetIdAcceptResponsePacket Set(int id = 0)
        {
            Id = id;
            return this;
        }
    }
}