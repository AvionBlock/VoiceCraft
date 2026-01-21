using System.Collections.Generic;

namespace VoiceCraft.Network.Packets.McHttpPackets;

public class McHttpUpdatePacket
{
    public List<string> Packets { get; set; } = [];
}