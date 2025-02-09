using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class EntityRemovedPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.EntityRemoved;
        
        public Guid Id { get; set; }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetGuid();
        }
    }
}