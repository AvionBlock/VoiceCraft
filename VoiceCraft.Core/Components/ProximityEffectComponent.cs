using Friflo.Engine.ECS;

namespace VoiceCraft.Core.Components
{
    public class ProximityEffectComponent : IComponent
    {
        public ulong Bitmask;
        public uint MinRange;
        public uint MaxRange;
    }
}