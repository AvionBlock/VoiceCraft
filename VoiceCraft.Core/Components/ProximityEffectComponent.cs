namespace VoiceCraft.Core.Components
{
    public struct ProximityEffectComponent : IAudioEffect
    {
        public uint Bitmask { get; set; }
        public uint MinRange { get; set; }
        public uint MaxRange { get; set; }
    }
}