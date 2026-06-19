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
        var eventType = (EventType)reader.GetByte();
        Set(IMcApiEventPacket.FromReader(eventType, reader));
    }

    public McApiEventRequestPacket Set(IMcApiEventPacket? @event)
    {
        if (Event != null)
        {
            IMcApiEventPacket.ReturnPacket(Event);
        }

        Event = @event;
        return this;
    }
}