namespace VoiceCraft.Core.Components
{
    public struct AudioListenerComponent : IAudioInput
    {
        public string EnvironmentId;
        public ulong Bitmask;
    }
}