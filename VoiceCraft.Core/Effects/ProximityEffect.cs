namespace VoiceCraft.Core.Effects
{
    public struct ProximityEffect : IAudioEffect
    {
        public uint Bitmask { get; set; }
        public uint MinRange { get; set; }
        public uint MaxRange { get; set; }
    }
}