using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Server.Systems
{
    public class AudioEffectSystem : IDisposable
    {
        public event Action<byte, IAudioEffect>? OnEffectSet;
        public event Action<byte, IAudioEffect>? OnEffectRemoved;

        public IEnumerable<KeyValuePair<byte, IAudioEffect>> Effects => _audioEffects;
        private readonly Dictionary<byte, IAudioEffect> _audioEffects = new();

        public void AddEffect(IAudioEffect effect)
        {
            var id = GetLowestAvailableId();
            if(_audioEffects.TryAdd(id, effect))
                throw new InvalidOperationException(Locales.Locales.AudioEffectSystem_FailedToAddEffect);
        }

        public void SetEffect(IAudioEffect effect, byte index)
        {
            if(!_audioEffects.TryAdd(index, effect))
                _audioEffects[index] = effect;
            OnEffectSet?.Invoke(index, effect);
        }

        public void RemoveEffect(byte index)
        {
            if (!_audioEffects.Remove(index, out var effect))
                throw new InvalidOperationException(Locales.Locales.AudioEffectSystem_FailedToRemoveEffect);
            effect.Dispose();
            OnEffectRemoved?.Invoke(index, effect);
        }
        
        private byte GetLowestAvailableId()
        {
            for(var i = byte.MinValue; i < byte.MaxValue; i++)
            {
                if(!_audioEffects.ContainsKey(i)) return i;
            }

            throw new InvalidOperationException(Locales.Locales.AudioEffectSystem_NoAvailableIdFound);
        }
        
        public void Dispose()
        {
            OnEffectSet = null;
            OnEffectRemoved = null;
            foreach (var effect in Effects)
            {
                effect.Value.Dispose();
            }
            _audioEffects.Clear();
            GC.SuppressFinalize(this);
        }
    }
}