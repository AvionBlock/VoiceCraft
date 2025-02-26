using Friflo.Engine.ECS;

namespace VoiceCraft.Core.Components
{
    public struct NetworkComponent : IComponent
    {
        public readonly uint NetworkId;

        public NetworkComponent(uint networkId)
        {
            NetworkId = networkId;
        }
    }
}