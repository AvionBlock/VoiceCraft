using LiteNetLib;

namespace VoiceCraft.Server
{
    public class VoiceCraftClient
    {
        public readonly NetPeer Peer;
        
        public VoiceCraftClient(NetPeer peer)
        {
            Peer = peer;
        }
    }
}