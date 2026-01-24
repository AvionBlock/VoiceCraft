using System;
using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Request;

public class McApiEntityAudioRequestPacket(
    int id = 0,
    ushort timestamp = 0,
    float loudness = 0f,
    int length = 0,
    byte[]? data = null)
    : IMcApiPacket
{
    public McApiEntityAudioRequestPacket() : this(0)
    {
    }

    public int Id { get; private set; } = id;
    public ushort Timestamp { get; private set; } = timestamp;
    public float FrameLoudness { get; private set; } = loudness;
    public int Length { get; private set; } = length;
    public byte[] Buffer { get; private set; } = data ?? Array.Empty<byte>();

    public McApiPacketType PacketType => McApiPacketType.EntityAudioRequest;

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

    public McApiEntityAudioRequestPacket Set(int id = 0, ushort timestamp = 0, float loudness = 0f, int length = 0,
        byte[]? data = null)
    {
        Id = id;
        Timestamp = timestamp;
        FrameLoudness = loudness;
        Length = length;
        Buffer = data ?? Array.Empty<byte>();
        return this;
    }
}