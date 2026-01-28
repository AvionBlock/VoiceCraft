using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.McApiPackets.Event;

public class McApiOnEntityAudioReceivedPacket(int id, ushort timestamp, float loudness) : IMcApiPacket
{
    public McApiOnEntityAudioReceivedPacket() : this(0, 0, 0.0f)
    {
    }

    public int Id { get; private set; } = id;
    public ushort Timestamp { get; private set; } = timestamp;
    public float FrameLoudness { get; private set; } = loudness;

    public McApiPacketType PacketType => McApiPacketType.OnEntityAudioReceived;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
        writer.Put(Timestamp);
        writer.Put(FrameLoudness);
    }

    public void Deserialize(NetDataReader reader)
    {
        Id = reader.GetInt();
        Timestamp = reader.GetUShort();
        FrameLoudness = Math.Clamp(reader.GetFloat(), 0f, 1f);
    }

    public McApiOnEntityAudioReceivedPacket Set(int id = 0, ushort timestamp = 0, float loudness = 0f)
    {
        Id = id;
        Timestamp = timestamp;
        FrameLoudness = loudness;
        return this;
    }
}