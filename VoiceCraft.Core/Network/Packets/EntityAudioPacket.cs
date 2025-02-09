using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class EntityAudioPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.EntityAudio;
        
        public Guid Id { get; set; } = Guid.Empty;
        public uint Timestamp { get; set; }
        public int DataLength { get; set; }
        public byte[]? Data { get; set; }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Timestamp);
            writer.Put(DataLength);
            if (Data != null && DataLength > 0)
                writer.Put(Data, 0, DataLength);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetGuid();
            Timestamp = reader.GetUInt();
            DataLength = reader.GetInt();
            if (DataLength <= 0) return;
            Data = new byte[DataLength];
            reader.GetBytes(Data, DataLength);
        }
    }
}