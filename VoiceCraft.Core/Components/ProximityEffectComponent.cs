using Friflo.Engine.ECS;

namespace VoiceCraft.Core.Components
{
    public struct ProximityEffectComponent : IComponent, IAudioEffect
    {
        public uint Bitmask { get; set; }
        public uint MinRange;
        public uint MaxRange;
    }
}