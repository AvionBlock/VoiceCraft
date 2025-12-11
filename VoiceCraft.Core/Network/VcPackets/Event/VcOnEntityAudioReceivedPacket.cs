using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Event
{
    public class VcOnEntityAudioReceivedPacket : IVoiceCraftPacket
    {
        public VcOnEntityAudioReceivedPacket() : this(0, 0, 0.0f, 0, Array.Empty<byte>())
        {
        }

        public VcOnEntityAudioReceivedPacket(int id, ushort timestamp, float loudness, int length, byte[] data)
        {
            Id = id;
            Timestamp = timestamp;
            FrameLoudness = loudness;
            Length = length;
            Data = data;
        }

        public VcPacketType PacketType => VcPacketType.OnEntityAudioReceived;

        public int Id { get; private set; }
        public ushort Timestamp { get; private set; }
        public float FrameLoudness { get; private set; }
        public int Length { get; private set; }
        public byte[] Data { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Timestamp);
            writer.Put(FrameLoudness);
            writer.Put(Data, 0, Length);
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Timestamp = reader.GetUShort();
            FrameLoudness = Math.Clamp(reader.GetFloat(), 0f, 1f);
            Length = reader.AvailableBytes;
            //Fuck no. we aren't allocating anything higher than the expected amount of bytes (WHICH SHOULD BE COMPRESSED!).
            if (Length > Constants.MaximumEncodedBytes)
                throw new InvalidOperationException(
                    $"Array length exceeds maximum number of bytes per packet! Got {Length} bytes.");
            Data = new byte[Length];
            reader.GetBytes(Data, Length);
        }

        public VcOnEntityAudioReceivedPacket Set(int id = 0, ushort timestamp = 0, float loudness = 0f, int length = 0,
            byte[]? data = null)
        {
            Id = id;
            Timestamp = timestamp;
            FrameLoudness = loudness;
            Length = length;
            Data = data ?? Array.Empty<byte>();
            return this;
        }
    }
}