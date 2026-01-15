using System;

namespace VoiceCraft.Network.Packets.McHttpPackets;

public class McHttpUpdatePacket
{
    public string[] Packets { get; set; } = Array.Empty<string>();
}