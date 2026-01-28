using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Core.World;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnEntityCreatedPacket(int id, string name, bool muted, bool deafened)
    : IVoiceCraftPacket
{
    public VcOnEntityCreatedPacket() : this(0, string.Empty, false, false)
    {
    }

    public VcOnEntityCreatedPacket(VoiceCraftEntity entity) : this(entity.Id, entity.Name, entity.Muted, entity.Deafened)
    {
    }

    public int Id { get; private set; } = id;
    public string Name { get; private set; } = name;
    public bool Muted { get; private set; } = muted;
    public bool Deafened { get; private set; } = deafened;

    public virtual VcPacketType PacketType => VcPacketType.OnEntityCreated;

    public virtual void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
        writer.Put(Name, Constants.MaxStringLength);
        writer.Put(Muted);
        writer.Put(Deafened);
    }

    public virtual void Deserialize(NetDataReader reader)
    {
        Id = reader.GetInt();
        Name = reader.GetString(Constants.MaxStringLength);
        Muted = reader.GetBool();
        Deafened = reader.GetBool();
    }

    public void Set(int id = 0, string name = "", bool muted = false, bool deafened = false)
    {
        Id = id;
        Name = name;
        Muted = muted;
        Deafened = deafened;
    }

    public VcOnEntityCreatedPacket Set(VoiceCraftEntity entity)
    {
        Id = entity.Id;
        Name = entity.Name;
        Muted = entity.Muted;
        Deafened = entity.Deafened;
        return this;
    }
}