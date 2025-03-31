using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class AudioPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.Audio;
        public int Id { get; private set; }
        public uint Timestamp { get; private set; }
        public int Length { get; private set; }
        public byte[] Data { get; private set; }
        
        public AudioPacket(int id = 0, byte[]? data = null, int length = 0, uint timestamp = 0)
        {
            Id = id;
            Data = data ?? Array.Empty<byte>();
            Length = length;
            Timestamp = timestamp;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Timestamp);
            writer.Put(Length);
            writer.Put(Data, 0, Length);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Timestamp = reader.GetUInt();
            var length = reader.GetInt();
            //Fuck no. we aren't allocating anything higher than the expected amount of bytes (WHICH SHOULD BE COMPRESSED!).
            if (length > Constants.MaximumEncodedBytes)
                throw new InvalidOperationException("Array length exceeds maximum number of bytes per packet!");
            Data = new byte[length];
            reader.GetBytes(Data, length);
        }
    }
}