using System.Collections.ObjectModel;

namespace VoiceCraft.Core.Packets.MCComm;

public class AckUpdate : MCCommPacket
{
    public override byte PacketId => (byte)MCCommPacketTypes.AckUpdate;

    public Collection<string> SpeakingPlayers { get; } = new Collection<string>();
}
