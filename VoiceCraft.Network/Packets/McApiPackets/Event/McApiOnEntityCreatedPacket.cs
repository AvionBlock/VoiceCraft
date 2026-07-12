using System;
using System.Numerics;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Core.World;

namespace VoiceCraft.Network.Packets.McApiPackets.Event;

public class McApiOnEntityCreatedPacket(int id, float loudness, DateTime lastSpoke) : IMcApiEventPacket
{
    public McApiOnEntityCreatedPacket() : this(0, 0.0f, DateTime.MinValue)
    {
    }

    public McApiOnEntityCreatedPacket(VoiceCraftEntity entity) : this(entity.Id, entity.Loudness, entity.LastSpoke)
    {
    }

    public virtual EventType EventType => EventType.OnEntityCreated;
    public int Id { get; private set; } = id;
    public float Loudness { get; private set; } = loudness;
    public DateTime LastSpoke { get; private set; } = lastSpoke;

    public virtual void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
        writer.Put(Loudness);
        writer.Put(LastSpoke.Ticks);
    }

    public virtual void Deserialize(NetDataReader reader)
    {
        Id = reader.GetInt();
        Loudness = reader.GetFloat();
        LastSpoke = new DateTime(reader.GetLong());
    }

    public virtual void Return()
    {
        PacketPool<McApiOnEntityCreatedPacket>.Return(this);
    }

    public void Set(int id = 0, float loudness = 0.0f, DateTime lastSpoke = new())
    {
        Id = id;
        Loudness = loudness;
        LastSpoke = lastSpoke;
    }

    public void Set(VoiceCraftEntity entity)
    {
        Set(entity.Id, entity.Loudness, entity.LastSpoke);
    }
}