using System;
using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Event;

public class McApiOnEntityAudioReceivedPacket : IMcApiPacket
{
    public McApiOnEntityAudioReceivedPacket() : this(0, 0, 0.0f)
    {
    }

    public McApiOnEntityAudioReceivedPacket(int id, ushort timestamp, float loudness)
    {
        Id = id;
        Timestamp = timestamp;
        FrameLoudness = loudness;
    }

    public int Id { get; private set; }
    public ushort Timestamp { get; private set; }
    public float FrameLoudness { get; private set; }

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