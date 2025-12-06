using System.Collections.Generic;

namespace VoiceCraft.Core.Packets.MCComm;

public class GetChannels : MCCommPacket
{
    public override byte PacketId => (byte)MCCommPacketTypes.GetChannels;
    public Dictionary<byte, Channel> Channels { get; } = new Dictionary<byte, Channel>();
}
