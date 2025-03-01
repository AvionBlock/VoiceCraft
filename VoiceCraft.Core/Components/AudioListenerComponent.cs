using Friflo.Engine.ECS;

namespace VoiceCraft.Core.Components
{
    public struct AudioListenerComponent : IComponent, IAudioInput
    {
        public string EnvironmentId;
        public ulong Bitmask;
    }
}