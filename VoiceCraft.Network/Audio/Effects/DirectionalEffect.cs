using System;
using System.Text.Json.Serialization;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Network.Audio.Effects
{
    public class DirectionalEffect : IAudioEffect
    {
        public static int SampleRate => Constants.SampleRate;
        
        public EffectType EffectType => EffectType.Directional;
        
        [JsonIgnore]
        public ushort Bitmask { get; set; }
        
        public event Action<IAudioEffect>? OnDisposed;
        
        public float WetDry
        {
            get;
            set => field = Math.Clamp(value, 0.0f, 1.0f);
        } = 1.0f;
        
        public IAudioEffectProcessor GetProcessor(VoiceCraftEntity entity) =>
            new DirectionalEffectProcessor(this, entity);

        public void Update(IAudioEffect audioEffect)
        {
            if(audioEffect is not DirectionalEffect directionalEffect)
                throw new ArgumentException("Unexpected Audio Effect Type!", nameof(audioEffect));
            Bitmask = directionalEffect.Bitmask;
            WetDry = directionalEffect.WetDry;
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(WetDry);
        }

        public void Deserialize(NetDataReader reader)
        {
            WetDry = reader.GetFloat();
        }

        public void Dispose()
        {
            try
            {
                OnDisposed?.Invoke(this);
            }
            finally
            {
                OnDisposed = null;
                GC.SuppressFinalize(this);
            }
        }
    }
    
    public class DirectionalEffectProcessor : IAudioEffectProcessor
    {
        private readonly DirectionalEffect _effect;
        private readonly SampleLerpVolume _lerpVolume1;
        private readonly SampleLerpVolume _lerpVolume2;
        public IAudioEffect Effect => _effect;
        public VoiceCraftEntity Entity { get; }
        public event Action<IAudioEffectProcessor>? OnDisposed;

        public DirectionalEffectProcessor(DirectionalEffect effect, VoiceCraftEntity entity)
        {
            _effect = effect;
            Entity = entity;
            _lerpVolume1 = new SampleLerpVolume(Constants.SampleRate, TimeSpan.FromMilliseconds(20));
            _lerpVolume2 = new SampleLerpVolume(Constants.SampleRate, TimeSpan.FromMilliseconds(20));
            Effect.OnDisposed += _ => Dispose();
        }

        public void Process(VoiceCraftEntity to, Span<float> buffer)
        {
            var bitmask = Entity.TalkBitmask & to.ListenBitmask & Entity.EffectBitmask & to.EffectBitmask;
            if ((bitmask & Effect.Bitmask) == 0) return;

            var rot = (float)(Math.Atan2(to.Position.Z - Entity.Position.Z, to.Position.X - Entity.Position.X) -
                              to.Rotation.Y * Math.PI / 180);
            var left = (float)Math.Max(0.5 - Math.Cos(rot) * 0.5, 0.2);
            var right = (float)Math.Max(0.5 + Math.Cos(rot) * 0.5, 0.2);
            
            _lerpVolume1.TargetVolume = left;
            _lerpVolume2.TargetVolume = right;

            for (var i = 0; i < buffer.Length; i += 2)
            {
                var leftOutput = _lerpVolume1.Transform(buffer[i]);
                var rightOutput = _lerpVolume2.Transform(buffer[i + 1]);
                
                buffer[i] = leftOutput * _effect.WetDry + buffer[i] * (1.0f - _effect.WetDry);
                buffer[i + 1] = rightOutput * _effect.WetDry + buffer[i + 1] * (1.0f - _effect.WetDry);
                
                _lerpVolume1.Step();
                _lerpVolume2.Step();
            }
        }

        public void Dispose()
        {
            try
            {
                OnDisposed?.Invoke(this);
            }
            finally
            {
                GC.SuppressFinalize(this);
            }
        }
    }
    
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(DirectionalEffect), GenerationMode = JsonSourceGenerationMode.Metadata)]
    public partial class DirectionalEffectGenerationContext : JsonSerializerContext;
}