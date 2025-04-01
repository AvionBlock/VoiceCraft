using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class AudioPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.Audio;
        public int Id { get; private set; }
        public uint Timestamp { get; private set; }
        public ushort Length { get; private set; }
        public byte[] Data { get; private set; }
        
        public AudioPacket(int id = 0, uint timestamp = 0, ushort length = 0, byte[]? data = null)
        {
            Id = id;
            Timestamp = timestamp;
            Length = length;
            Data = data ?? Array.Empty<byte>();
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Timestamp);
            writer.Put(Length);
            writer.PutBytesWithLength(Data, 0, Length);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Timestamp = reader.GetUInt();
            Length = reader.GetUShort();
            Data = reader.GetBytesWithLength();
            /*
            //Fuck no. we aren't allocating anything higher than the expected amount of bytes (WHICH SHOULD BE COMPRESSED!).
            if (Length > Constants.MaximumEncodedBytes)
                throw new InvalidOperationException($"Array length exceeds maximum number of bytes per packet! Got {Length} bytes.");
            Data = new byte[Length];
            reader.GetBytes(Data, Length);
            */
        }
    }
}