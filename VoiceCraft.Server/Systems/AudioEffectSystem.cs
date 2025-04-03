using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Server.Systems
{
    public class AudioEffectSystem
    {
        public event Action<IAudioEffect, byte>? OnEffectSet;
        public event Action<IAudioEffect, byte>? OnEffectRemoved;

        public IEnumerable<IAudioEffect?> Effects => _audioEffects;
        
        private readonly IAudioEffect?[] _audioEffects = new IAudioEffect?[byte.MaxValue]; //Can only have 256 effects

        public bool AddEffect(IAudioEffect effect)
        {
            for (byte i = 0; i < _audioEffects.Length; i++)
            {
                if (_audioEffects[i] != null) continue;
                _audioEffects[i] = effect;
                OnEffectSet?.Invoke(effect, i);
                return true;
            }

            return false;
        }

        public void SetEffect(IAudioEffect effect, byte index)
        {
            _audioEffects[index] = effect;
            OnEffectSet?.Invoke(effect, index);
        }

        public bool RemoveEffect(byte index)
        {
            var effect = _audioEffects[index];
            if(effect == null) return false;
            _audioEffects[index] = null;
            OnEffectRemoved?.Invoke(effect, index);
            return true;
        }
    }
}