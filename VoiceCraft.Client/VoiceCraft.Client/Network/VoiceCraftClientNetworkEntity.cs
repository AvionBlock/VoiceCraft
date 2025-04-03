using LiteNetLib;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Network
{
    public class VoiceCraftClientNetworkEntity(NetPeer netPeer) : VoiceCraftEntity((short)netPeer.RemoteId)
    {
        public NetPeer NetPeer { get; } = netPeer;
    }
}