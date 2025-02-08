using System;
using LiteNetLib.Utils;
using VoiceCraft.Core.Data;
using VoiceCraft.Core.Data.Packets;

namespace VoiceCraft.Core.Network.Packets
{
    public class EntityCreatedPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.EntityCreated;
        
        public Guid Id { get; set; }
        public string Name { get; set; }
        
        public override void Serialize(NetDataWriter writer)
        {
            throw new System.NotImplementedException();
        }

        public override void Deserialize(NetDataReader reader)
        {
            throw new System.NotImplementedException();
        }
    }
}