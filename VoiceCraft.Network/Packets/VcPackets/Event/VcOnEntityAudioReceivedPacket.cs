using System;
using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEntityAudioReceivedPacket(int id, ushort timestamp, float loudness, int length, byte[] buffer)
    : IVoiceCraftPacket
{
    public VcOnEntityAudioReceivedPacket() : this(0, 0, 0.0f, 0, [])
    {
    }

    public int Id { get; private set; } = id;
    public ushort Timestamp { get; private set; } = timestamp;
    public float FrameLoudness { get; private set; } = loudness;
    public int Length { get; private set; } = length;
    public byte[] Buffer { get; private set; } = buffer;

    public VcPacketType PacketType => VcPacketType.OnEntityAudioReceived;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
        writer.Put(Timestamp);
        writer.Put(FrameLoudness);
        writer.Put(Buffer, 0, Length);
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
        Buffer = new byte[Length];
        reader.GetBytes(Buffer, Length);
    }

    public VcOnEntityAudioReceivedPacket Set(int id = 0, ushort timestamp = 0, float loudness = 0f, int length = 0,
        byte[]? data = null)
    {
        Id = id;
        Timestamp = timestamp;
        FrameLoudness = loudness;
        Length = length;
        Buffer = data ?? [];
        return this;
    }
}