using VoiceCraft.Core.ECS;

namespace VoiceCraft.Core.Components
{
    public class AudioListenerComponent : Component
    {
        public string EnvironmentId = string.Empty;
        public ulong Bitmask;

        public AudioListenerComponent(Entity entity) : base(entity)
        { }
    }
}