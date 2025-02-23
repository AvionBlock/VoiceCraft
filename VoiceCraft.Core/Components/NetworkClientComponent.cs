using LiteNetLib;
using VoiceCraft.Core.ECS;

namespace VoiceCraft.Core.Components
{
    public class NetworkClientComponent : Component
    {
        public readonly NetPeer Peer;
        
        public NetworkClientComponent(Entity entity, NetPeer peer) : base(entity)
        {
            Peer = peer;
        }
    }
}