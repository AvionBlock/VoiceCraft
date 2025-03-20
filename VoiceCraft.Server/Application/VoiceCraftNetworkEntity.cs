using LiteNetLib;
using VoiceCraft.Core;

namespace VoiceCraft.Server.Application
{
    public class VoiceCraftNetworkEntity(NetPeer netPeer) : VoiceCraftEntity(netPeer.Id)
    {
        public NetPeer NetPeer { get; } = netPeer;
    }
}