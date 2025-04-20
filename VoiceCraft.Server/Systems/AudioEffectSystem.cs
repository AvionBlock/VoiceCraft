using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Server.Systems
{
    public class AudioEffectSystem
    {
        public event Action<byte, IAudioEffect>? OnEffectSet;
        public event Action<byte, IAudioEffect>? OnEffectRemoved;

        public IEnumerable<KeyValuePair<byte, IAudioEffect>> Effects => _audioEffects;
        private readonly Dictionary<byte, IAudioEffect> _audioEffects = new();

        public void AddEffect(IAudioEffect effect)
        {
            var id = GetLowestAvailableId();
            if(_audioEffects.TryAdd(id, effect))
                throw new InvalidOperationException("Failed to add effect!");
        }

        public void SetEffect(IAudioEffect effect, byte index)
        {
            if(!_audioEffects.TryAdd(index, effect))
                _audioEffects[index] = effect;
            OnEffectSet?.Invoke(index, effect);
        }

        public bool RemoveEffect(byte index)
        {
            if (!_audioEffects.Remove(index, out var effect)) return false;
            OnEffectRemoved?.Invoke(index, effect);
            return true;
        }
        
        private byte GetLowestAvailableId()
        {
            for(var i = byte.MinValue; i < byte.MaxValue; ++i)
            {
                if(!_audioEffects.ContainsKey(i)) return i;
            }

            throw new InvalidOperationException("Could not find an available id!");
        }
    }
}