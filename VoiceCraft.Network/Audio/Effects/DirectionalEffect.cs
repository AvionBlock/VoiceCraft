using System;
using System.Text.Json.Serialization;
using LiteNetLib.Utils;
using VoiceCraft.Core;
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
    
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(DirectionalEffect), GenerationMode = JsonSourceGenerationMode.Metadata)]
    public partial class DirectionalEffectGenerationContext : JsonSerializerContext;
}