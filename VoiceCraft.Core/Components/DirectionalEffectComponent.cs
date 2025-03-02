namespace VoiceCraft.Core.Components
{
    public class DirectionalEffectComponent : IAudioEffect
    {
        public uint Bitmask { get; set; }
        public uint XRotation { get; set; }
        public uint YRotation { get; set; }
        public uint XRange { get; set; }
        public uint YRange { get; set; }
    }
}