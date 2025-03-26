using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class AudioPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.Audio;
        public int NetworkId { get; set; }
        public int Length { get; set; }
        public uint Timestamp { get; set; }
        public byte[] Data { get; set; }
        
        public AudioPacket(int networkId, byte[] data, int length, uint timestamp)
        {
            NetworkId = networkId;
            Data = data;
            Length = length;
            Timestamp = timestamp;
        }
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(NetworkId);
            writer.Put(Timestamp);
            writer.Put(Length);
            writer.Put(Data, 0, Length);
        }

        public override void Deserialize(NetDataReader reader)
        {
            NetworkId = reader.GetInt();
            Timestamp = reader.GetUInt();
            Length = reader.GetInt();
            //Fuck no. we aren't allocating anything higher than the expected amount of bytes (WHICH SHOULD BE COMPRESSED!).
            if (Length > Constants.MaximumEncodedBytes)
                throw new InvalidOperationException("Array length exceeds maximum number of bytes per packet!");
            Data = new byte[Length];
        }
    }
}