using Friflo.Engine.ECS;

namespace VoiceCraft.Core.Components
{
    public struct AudioSourceComponent : IComponent, IAudioOutput
    {
        public string EnvironmentId;
        public IAudioInput AudioInput;
        public ulong Bitmask;
        public string Name;
    }
}