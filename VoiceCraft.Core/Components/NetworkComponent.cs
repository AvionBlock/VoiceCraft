using Friflo.Engine.ECS;
using LiteNetLib;

namespace VoiceCraft.Core.Components
{
    public struct NetworkComponent : IComponent
    {
        public readonly uint NetworkId;
        public readonly NetPeer? Peer;

        public NetworkComponent(uint networkId, NetPeer? peer = null)
        {
            NetworkId = networkId;
            Peer = peer;
        }
    }
}