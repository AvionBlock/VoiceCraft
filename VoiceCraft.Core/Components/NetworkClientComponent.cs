using Friflo.Engine.ECS;
using LiteNetLib;

namespace VoiceCraft.Core.Components
{
    public struct NetworkClientComponent : IComponent
    {
        public readonly NetPeer Peer;

        public NetworkClientComponent(NetPeer peer)
        {
            Peer = peer;
        }
    }
}