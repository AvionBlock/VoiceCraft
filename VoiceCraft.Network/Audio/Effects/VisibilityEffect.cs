using System;
using System.Text.Json.Serialization;
using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Network.Audio.Effects
{
    public class VisibilityEffect : IAudioEffect, IVisible
    {
        public EffectType EffectType => EffectType.Visibility;

        public void Process(VoiceCraftEntity from, VoiceCraftEntity to, ushort effectBitmask, Span<float> buffer)
        {
        }

        public void Serialize(NetDataWriter writer)
        {
        }

        public void Deserialize(NetDataReader reader)
        {
        }

        public void Reset()
        {
            //Nothing to reset
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public bool Visibility(VoiceCraftEntity from, VoiceCraftEntity to, ushort effectBitmask)
        {
            var bitmask = from.TalkBitmask & to.ListenBitmask & from.EffectBitmask & to.EffectBitmask;
            if ((effectBitmask & bitmask) == 0) return true; //Disabled, is visible by default.

            return !string.IsNullOrWhiteSpace(from.WorldId) && !string.IsNullOrWhiteSpace(to.WorldId) &&
                   from.WorldId == to.WorldId;
        }
    }
    
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(VisibilityEffect), GenerationMode = JsonSourceGenerationMode.Metadata)]
    public partial class VisibilityEffectGenerationContext : JsonSerializerContext;
}