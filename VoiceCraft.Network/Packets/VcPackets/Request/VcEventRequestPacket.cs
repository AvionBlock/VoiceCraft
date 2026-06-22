using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcEventRequestPacket(IVoiceCraftEventPacket? @event) : IVoiceCraftPacket
{
    public VcEventRequestPacket() : this(null)
    {
    }

    public VcPacketType PacketType => VcPacketType.EventRequest;
    public EventType EventType => Event?.EventType ?? EventType.None;
    public IVoiceCraftEventPacket? Event { get; private set; } = @event;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)EventType);
        if (Event != null)
            writer.Put(Event);
    }

    public void Deserialize(NetDataReader reader)
    {
        if (Event != null)
        {
            Event.Return();
            Event = null;
        }
        
        var eventType = (EventType)reader.GetByte();
        Event = IVoiceCraftEventPacket.FromReader(eventType, reader);
    }

    public VcEventRequestPacket Set(IVoiceCraftEventPacket? @event)
    {
        Event = @event;
        return this;
    }

    public void Return()
    {
    }
}