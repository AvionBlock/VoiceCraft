using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.McApiPackets.Request;

public class McApiEventRequestPacket(IMcApiEventPacket? @event) : IMcApiPacket
{
    public McApiEventRequestPacket() : this(null)
    {
    }

    public McApiPacketType PacketType => McApiPacketType.EventRequest;
    public EventType EventType => Event?.EventType ?? EventType.None;
    public IMcApiEventPacket? Event { get; private set; } = @event;

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
            //Return the original event packet.
            Event.Return();
            Event = null;
        }
        
        var eventType = (EventType)reader.GetByte();
        Event = IMcApiEventPacket.FromReader(eventType, reader);
    }
    
    public void Return()
    {
        PacketPool<McApiEventRequestPacket>.Return(this);
    }

    public void Set(IMcApiEventPacket? @event)
    {
        Event = @event;
    }
}