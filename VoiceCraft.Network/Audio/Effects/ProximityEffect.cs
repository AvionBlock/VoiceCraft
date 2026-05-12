using System;
using System.Numerics;
using System.Text.Json.Serialization;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Network.Audio.Effects
{
    public class ProximityEffect : IAudioEffect, IVisible
    {
        public static int SampleRate => Constants.SampleRate;
        
        public EffectType EffectType => EffectType.Proximity;

        [JsonIgnore]
        public ushort Bitmask { get; set; }
        
        public event Action? OnDisposed;

        public float MinRange { get; set; }
        public float MaxRange { get; set; }
        
        public float WetDry
        {
            get;
            set => field = Math.Clamp(value, 0.0f, 1.0f);
        } = 1.0f;
        
        public void Update(IAudioEffect audioEffect)
        {
            if(audioEffect is not ProximityEffect proximityEffect)
                throw new ArgumentException("Unexpected Audio Effect Type!", nameof(audioEffect));
            Bitmask = proximityEffect.Bitmask;
            MinRange = proximityEffect.MinRange;
            MaxRange = proximityEffect.MaxRange;
            WetDry = proximityEffect.WetDry;
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(MinRange);
            writer.Put(MaxRange);
            writer.Put(WetDry);
        }

        public void Deserialize(NetDataReader reader)
        {
            MinRange = reader.GetFloat();
            MaxRange = reader.GetFloat();
            WetDry = reader.GetFloat();
        }

        public bool Visibility(VoiceCraftEntity from, VoiceCraftEntity to, ushort effectBitmask)
        {
            var bitmask = from.TalkBitmask & to.ListenBitmask & from.EffectBitmask & to.EffectBitmask;
            if ((bitmask & effectBitmask) == 0) return true; //Proximity checking disabled.
            var distance = Vector3.Distance(from.Position, to.Position);
            return distance <= MaxRange;
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
    [JsonSerializable(typeof(ProximityEffect), GenerationMode = JsonSourceGenerationMode.Metadata)]
    public partial class ProximityEffectGenerationContext : JsonSerializerContext;
}