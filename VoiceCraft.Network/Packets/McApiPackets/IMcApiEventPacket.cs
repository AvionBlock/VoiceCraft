namespace VoiceCraft.Network.Packets.McApiPackets;

public interface IMcApiEventPacket : IMcApiPacket
{
    McApiEventType EventType { get; }
}