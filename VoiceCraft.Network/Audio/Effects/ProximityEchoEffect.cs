using System;
using System.Text.Json.Serialization;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Network.Audio.Effects
{
    public class ProximityEchoEffect : IAudioEffect
    {
        public static int SampleRate => Constants.SampleRate;
        
        public EffectType EffectType => EffectType.ProximityEcho;
        
        [JsonIgnore]
        public ushort Bitmask { get; set; }
        
        public event Action<IAudioEffect>? OnDisposed;

        public float Delay
        {
            get => field / SampleRate;
            set => field = SampleRate * Math.Clamp(value, 0.0f, 10.0f);
        }

        public float Range
        {
            get;
            set => field = Math.Max(value, 0.0f);
        }
        
        public float WetDry
        {
            get;
            set => field = Math.Clamp(value, 0.0f, 1.0f);
        } = 1.0f;
        
        public ProximityEchoEffect()
        {
            Delay = 0.5f;
        }
        
        public void Update(IAudioEffect audioEffect)
        {
            if(audioEffect is not ProximityEchoEffect proximityEchoEffect)
                throw new ArgumentException("Unexpected Audio Effect Type!", nameof(audioEffect));
            Bitmask = proximityEchoEffect.Bitmask;
            Delay = proximityEchoEffect.Delay;
            Range = proximityEchoEffect.Range;
            WetDry = proximityEchoEffect.WetDry;
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Delay);
            writer.Put(Range);
            writer.Put(WetDry);
        }

        public void Deserialize(NetDataReader reader)
        {
            Delay = reader.GetFloat();
            Range = reader.GetFloat();
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
    
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(ProximityEchoEffect), GenerationMode = JsonSourceGenerationMode.Metadata)]
    public partial class ProximityEchoEffectGenerationContext : JsonSerializerContext;
}