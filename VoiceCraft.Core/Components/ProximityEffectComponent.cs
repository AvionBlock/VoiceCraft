using VoiceCraft.Core.ECS;

namespace VoiceCraft.Core.Components
{
    public class ProximityEffectComponent : Component
    {
        public ulong Bitmask;
        public uint MinRange;
        public uint MaxRange;

        public ProximityEffectComponent(Entity entity) : base(entity)
        { }
    }
}