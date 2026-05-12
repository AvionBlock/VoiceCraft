using System;
using System.Text.Json.Serialization;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Network.Audio.Effects
{
    public class MuffleEffect : IAudioEffect
    {
        public static int SampleRate => Constants.SampleRate;

        public EffectType EffectType => EffectType.Muffle;
        
        [JsonIgnore]
        public ushort Bitmask { get; set; }
        
        public event Action? OnDisposed;
        
        public float WetDry
        {
            get;
            set => field = Math.Clamp(value, 0.0f, 1.0f);
        } = 1.0f;
        
        public void Update(IAudioEffect audioEffect)
        {
            if(audioEffect is not MuffleEffect muffleEffect)
                throw new ArgumentException("Unexpected Audio Effect Type!", nameof(audioEffect));
            Bitmask = muffleEffect.Bitmask;
            WetDry = muffleEffect.WetDry;
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
                OnDisposed?.Invoke();
            }
            finally
            {
                OnDisposed = null;
                GC.SuppressFinalize(this);
            }
        }
    }
    
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(MuffleEffect), GenerationMode = JsonSourceGenerationMode.Metadata)]
    public partial class MuffleEffectGenerationContext : JsonSerializerContext;
}