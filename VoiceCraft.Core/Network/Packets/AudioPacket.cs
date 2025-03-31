using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class AudioPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.Audio;
        public int NetworkId { get; private set; }
        public uint Timestamp { get; private set; }
        public byte[] Data { get; private set; }
        
        public AudioPacket(int networkId, byte[] data, uint timestamp)
        {
            NetworkId = networkId;
            Data = data;
            Timestamp = timestamp;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(NetworkId);
            writer.Put(Timestamp);
            writer.Put(Data.Length);
            writer.Put(Data);
        }

        public override void Deserialize(NetDataReader reader)
        {
            NetworkId = reader.GetInt();
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