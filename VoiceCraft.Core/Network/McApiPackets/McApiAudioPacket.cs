using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets
{
    public class McApiAudioPacket : McApiPacket
    {
        public McApiAudioPacket(string sessionToken = "", int id = 0, uint timestamp = 0, float loudness = 0f,
            int length = 0, byte[]? data = null)
        {
            SessionToken = sessionToken;
            Id = id;
            Timestamp = timestamp;
            FrameLoudness = loudness;
            Length = length;
            Data = data ?? Array.Empty<byte>();
        }

        public override McApiPacketType PacketType => McApiPacketType.Audio;

        public string SessionToken { get; private set; }
        public int Id { get; private set; }
        public uint Timestamp { get; private set; }
        public float FrameLoudness { get; private set; }
        public int Length { get; private set; }
        public byte[] Data { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(SessionToken, Constants.MaxStringLength);
            writer.Put(Id);
            writer.Put(Timestamp);
            writer.Put(FrameLoudness);
            writer.Put(Data, 0, Length);
        }

        public override void Deserialize(NetDataReader reader)
        {
            SessionToken = reader.GetString(Constants.MaxStringLength);
            Id = reader.GetInt();
            Timestamp = reader.GetUInt();
            FrameLoudness = Math.Clamp(reader.GetFloat(), 0f, 1f);
            Length = reader.AvailableBytes;
            //Fuck no. we aren't allocating anything higher than the expected amount of bytes (WHICH SHOULD BE COMPRESSED!).
            if (Length > Constants.MaximumEncodedBytes)
                throw new InvalidOperationException(
                    $"Array length exceeds maximum number of bytes per packet! Got {Length} bytes.");
            Data = new byte[Length];
            reader.GetBytes(Data, Length);
        }
    }
}