using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Network.Packets;
using VoiceCraft.Server.Application;

namespace VoiceCraft.Server.Systems
{
    public class AudioEffectSystem(VoiceCraftServer server)
    {
        public event Action<IAudioEffect>? OnEffectSet;
        public event Action<IAudioEffect>? OnEffectRemoved;

        public IEnumerable<IAudioEffect?> Effects => _audioEffects;
        
        private readonly IAudioEffect?[] _audioEffects = new IAudioEffect?[byte.MaxValue]; //Can only have 256 effects
        private readonly NetworkSystem _networkSystem = server.NetworkSystem;

        public bool AddEffect(IAudioEffect effect)
        {
            for (byte i = 0; i < _audioEffects.Length; i++)
            {
                if (_audioEffects[i] != null) continue;
                _audioEffects[i] = effect;
                _networkSystem.Broadcast(new SetEffectPacket(i, effect));
                OnEffectSet?.Invoke(effect);
                return true;
            }

            return false;
        }

        public void SetEffect(IAudioEffect effect, byte index)
        {
            _audioEffects[index] = effect;
            _networkSystem.Broadcast(new SetEffectPacket(index, effect));
            OnEffectSet?.Invoke(effect);
        }

        public bool RemoveEffect(byte index)
        {
            var effect = _audioEffects[index];
            if(effect == null) return false;
            _audioEffects[index] = null;
            _networkSystem.Broadcast(new RemoveEffectPacket(index));
            OnEffectRemoved?.Invoke(effect);
            return true;
        }
    }
}