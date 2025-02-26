using Friflo.Engine.ECS;

namespace VoiceCraft.Core.Components
{
    public struct AudioListenerComponent : IComponent
    {
        public string EnvironmentId;
        public ulong Bitmask;
    }
}