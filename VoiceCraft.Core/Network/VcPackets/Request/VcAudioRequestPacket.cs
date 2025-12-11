using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Request
{
    public class VcAudioRequestPacket : IVoiceCraftPacket
    {
        public VcAudioRequestPacket() : this(0)
        {
        }

        public VcAudioRequestPacket(ushort timestamp = 0, float loudness = 0f, int length = 0, byte[]? data = null)
        {
            Timestamp = timestamp;
            FrameLoudness = loudness;
            Length = length;
            Data = data ?? Array.Empty<byte>();
        }

        public VcPacketType PacketType => VcPacketType.AudioRequest;

        public ushort Timestamp { get; private set; }
        public float FrameLoudness { get; private set; }
        public int Length { get; private set; }
        public byte[] Data { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Timestamp);
            writer.Put(FrameLoudness);
            writer.Put(Data, 0, Length);
        }

        public void Deserialize(NetDataReader reader)
        {
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

        public VcAudioRequestPacket Set(ushort timestamp = 0, float loudness = 0f, int length = 0, byte[]? data = null)
        {
            Timestamp = timestamp;
            FrameLoudness = loudness;
            Length = length;
            Data = data ?? Array.Empty<byte>();
            return this;
        }
    }
}