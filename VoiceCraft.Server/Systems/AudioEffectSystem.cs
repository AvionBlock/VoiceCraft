using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Server.Application;

namespace VoiceCraft.Server.Systems
{
    public class AudioEffectSystem(VoiceCraftServer server)
    {
        public event Action<IAudioEffect>? OnEffectSet;
        public event Action<IAudioEffect>? OnEffectRemoved;
        
        private readonly IAudioEffect?[] _audioEffects = new IAudioEffect?[byte.MaxValue]; //Can only have 256 effects
        private readonly NetworkSystem _networkSystem = server.NetworkSystem;
        private readonly VoiceCraftWorld _world = server.World;

        public bool AddEffect(IAudioEffect effect)
        {
            for (var i = 0; i < _audioEffects.Length; i++)
            {
                if (_audioEffects[i] != null) continue;
                _audioEffects[i] = effect;
                OnEffectSet?.Invoke(effect);
                return true;
            }

            return false;
        }

        public void SetEffect(IAudioEffect effect, byte index)
        {
            _audioEffects[index] = effect;
            OnEffectSet?.Invoke(effect);
        }

        public bool RemoveEffect(byte index)
        {
            var effect = _audioEffects[index];
            if(effect == null) return false;
            _audioEffects[index] = null;
            OnEffectRemoved?.Invoke(effect);
            return true;
        }
    }
}