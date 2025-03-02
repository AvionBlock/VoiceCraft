namespace VoiceCraft.Core.Components
{
    public struct AudioSourceComponent : IAudioOutput
    {
        public string EnvironmentId;
        public IAudioInput AudioInput;
        public ulong Bitmask;
        public string Name;
    }
}