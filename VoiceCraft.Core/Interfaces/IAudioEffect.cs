using Arch.Core;

namespace VoiceCraft.Core.Interfaces
{
    public interface IAudioEffect
    {
        EffectBitmask Bitmask { get; }
        
        bool CanSeeEntity(Entity entity);

        void Process(byte[] buffer);
    }
}