using System.Collections.Generic;
using VoiceCraft.Core.Packets;

namespace VoiceCraft.Core
{
    public class DebugProperties
    {
        public bool LogExceptions { get; set; } = false;
        public bool LogInboundPackets { get; set; } = false;
        public bool LogOutboundPackets { get; set; } = false;
        public bool LogInboundMCCommPackets { get; set; } = false;
        public bool LogOutboundMCCommPackets { get; set; } = false;
        public List<VoiceCraftPacketTypes> InboundPacketFilter { get; set; } = [];
        public List<VoiceCraftPacketTypes> OutboundPacketFilter { get; set; } = [];
        public List<MCCommPacketTypes> InboundMCCommFilter { get; set; } = [];
        public List<MCCommPacketTypes> OutboundMCCommFilter { get; set; } = [];
    }
}