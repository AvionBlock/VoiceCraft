using System.Collections.Generic;

namespace VoiceCraft.Core.Network.McHttpPackets
{
    public class McHttpUpdate
    {
        public List<byte[]> Packets { get; set; } = new List<byte[]>();
    }
}