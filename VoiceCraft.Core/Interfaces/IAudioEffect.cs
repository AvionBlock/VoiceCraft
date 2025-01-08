namespace VoiceCraft.Core.Interfaces
{
    public interface IAudioEffect
    {
        EffectBitmask Bitmask { get; }

        void Process(byte[] buffer);
    }
}