using System;
using System.Collections.Generic;
using System.Numerics;
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
            Pcm16ToFloat(buffer, count, _floatBuffer);
            
            lock (_audioEffects)
            {
                foreach (var effect in _audioEffects)
                {
                    effect.Value.Process(entity, client, _floatBuffer, count);
                }
            }
            PcmFloatProximityVolume(_floatBuffer, count, entity, client);
            PcmFloatTo16(_floatBuffer, count, buffer);
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

        public void SetEffect(IAudioEffect effect, byte index)
        {
            lock (_audioEffects)
            {
                if (!_audioEffects.TryAdd(index, effect))
                    _audioEffects[index] = effect;
                OnEffectSet?.Invoke(index, effect);
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
        
        private static void PcmFloatProximityVolume(Span<float> srcBuffer, int count, VoiceCraftEntity from, VoiceCraftEntity to)
        {
            var bitmask = from.TalkBitmask & to.ListenBitmask;
            if ((bitmask & 1ul) == 0) return; //Not enabled.
            
            int? minRange = null;
            if(from.TryGetProperty<int>(PropertyKey.MinRange, out var fromMinRange))
                minRange = (int)fromMinRange;
            if(to.TryGetProperty<int>(PropertyKey.MinRange, out var toMinRange))
                minRange = Math.Max(minRange ?? 0, (int)toMinRange);
            
            int? maxRange = null;
            if(from.TryGetProperty<int>(PropertyKey.MaxRange, out var fromMaxRange))
                maxRange = (int)fromMaxRange;
            if(to.TryGetProperty<int>(PropertyKey.MaxRange, out var toMaxRange))
                maxRange = Math.Max(maxRange ?? 0, (int)toMaxRange);
            
            maxRange ??= from.World.MaxRange; //If maxRange is still null, use the world max range.
            minRange ??= from.World.MinRange; //If min range is still null, use the world min range.
            var range = (int)(maxRange - minRange);
            
            var distance = Vector3.Distance(from.Position, to.Position);
            if(range == 0) return; //Range is 0. Do not calculate division.
            var factor = 1f - Math.Clamp(distance / range, 0f, 1.0f);
            
            for (var i = 0; i < count; i++)
            {
                srcBuffer[i] = Math.Clamp(srcBuffer[i] * factor, -1f, 1f);
            }
        }
    }
}