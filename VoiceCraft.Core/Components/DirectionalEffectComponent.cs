using Friflo.Engine.ECS;

namespace VoiceCraft.Core.Components
{
    public struct DirectionalEffectComponent : IComponent, IAudioEffect
    {
        public uint Bitmask { get; set; }
        public uint XRotation;
        public uint YRotation;
        public uint XRange;
        public uint YRange;
    }
}