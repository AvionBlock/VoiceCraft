using VoiceCraft.Core.ECS;

namespace VoiceCraft.Core.Components
{
    public class DirectionalEffectComponent : Component
    {
        public ulong Bitmask;

        public DirectionalEffectComponent(Entity entity) : base(entity)
        { }
    }
}