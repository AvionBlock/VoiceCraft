using Friflo.Engine.ECS;

namespace VoiceCraft.Core.Components
{
    public struct AudioSourceComponent : IComponent
    {
        public string EnvironmentId;
        public ulong Bitmask;
        public string Name;
    }
}