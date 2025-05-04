using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Network.Systems
{
    public class AudioEffectSystem(VoiceCraftClient client) : IDisposable
    {
        public event Action<byte, IAudioEffect>? OnEffectSet;
        public event Action<byte, IAudioEffect>? OnEffectRemoved;

        public IEnumerable<KeyValuePair<byte, IAudioEffect>> Effects
        {
            get
            {
                lock(_audioEffects)
                    return _audioEffects;
            }
        }
        private readonly Dictionary<byte, IAudioEffect> _audioEffects = new();
        private float[] _floatBuffer = [];

        public void ProcessEffects(Span<short> buffer, int count, VoiceCraftEntity entity)
        {
            if(_floatBuffer.Length < count)
                _floatBuffer = new float[count];
            Pcm16ToFloat(buffer, count, _floatBuffer); //To float
            
            lock (_audioEffects)
            {
                foreach (var effect in _audioEffects)
                {
                    effect.Value.Process(entity, client, _floatBuffer, count);
                }
            }
            
            PcmFloatTo16(_floatBuffer, count, buffer); //To 16bit
        }

        public void AddEffect(IAudioEffect effect)
        {
            lock (_audioEffects)
            {
                var id = GetLowestAvailableId();
                if (_audioEffects.TryAdd(id, effect))
                    throw new InvalidOperationException("Failed to add effect!");
            }
        }

        public void SetEffect(byte index, IAudioEffect effect)
        {
            lock (_audioEffects)
            {
                if (!_audioEffects.TryAdd(index, effect))
                    _audioEffects[index] = effect;
                OnEffectSet?.Invoke(index, effect);
            }
        }

        public bool TryGetEffect(byte index, [NotNullWhen(true)] out IAudioEffect? effect)
        {
            lock (_audioEffects)
            {
                effect = _audioEffects.GetValueOrDefault(index);
                return effect != null;
            }
        }

        public void RemoveEffect(byte index)
        {
            lock (_audioEffects)
            {
                if (!_audioEffects.Remove(index, out var effect))
                    throw new InvalidOperationException("Failed to remove effect!");
                effect.Dispose();
                OnEffectRemoved?.Invoke(index, effect);
            }
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
        
        private byte GetLowestAvailableId()
        {
            for(var i = byte.MinValue; i < byte.MaxValue; i++)
            {
                if(!_audioEffects.ContainsKey(i)) return i;
            }

            throw new InvalidOperationException("Could not find an available id!");
        }

        private static void Pcm16ToFloat(Span<short> buffer, int count, Span<float> destBuffer)
        {
            for (var i = 0; i < count; i++)
            {
                destBuffer[i] = buffer[i] / (short.MaxValue + 1f);
            }
        }

        private static void PcmFloatTo16(Span<float> floatBuffer, int count, Span<short> buffer)
        {
            for (var i = 0; i < count; i++)
            {
                buffer[i] = (short)(floatBuffer[i] * short.MaxValue);
            }
        }
    }
}