using System.Collections.ObjectModel;

namespace VoiceCraft.Core.Packets.MCComm;

public class GetParticipants : MCCommPacket
{
    public override byte PacketId => (byte)MCCommPacketTypes.GetParticipants;
    public Collection<string> Players { get; } = new Collection<string>();
}
