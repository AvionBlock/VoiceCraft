using System;

namespace VoiceCraft.Core.Network.McHttpPackets
{
    public class McHttpUpdatePacket
    {
        public string[] Packets { get; set; } = Array.Empty<string>();
    }
}