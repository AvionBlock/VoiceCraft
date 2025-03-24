using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class AudioPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.Audio;
        public int Length { get; set; }
        public uint Timestamp { get; set; }
        public byte[] Data { get; set; }
        
        public AudioPacket(byte[] data, int length, uint timestamp)
        {
            Data = data;
            Length = length;
            Timestamp = timestamp;
        }
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Timestamp);
            writer.Put(Length);
            writer.Put(Data, 0, Length);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Timestamp = reader.GetUInt();
            Length = reader.GetInt();
            //Fuck no. we aren't allocating anything higher than the expected amount of bytes (WHICH SHOULD BE COMPRESSED). So we half it.
            if (Length > Constants.MaximumEncodedBytes)
                throw new InvalidOperationException("Array length exceeds maximum number of bytes per packet!");
            Data = new byte[Length];
        }
    }
}