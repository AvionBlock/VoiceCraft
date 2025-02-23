using VoiceCraft.Core.ECS;

namespace VoiceCraft.Core.Components
{
    public class AudioSourceComponent : Component
    {
        public string EnvironmentId = string.Empty;
        public ulong Bitmask;
        public string Name = string.Empty;

        public AudioSourceComponent(Entity entity) : base(entity)
        { }
    }
}