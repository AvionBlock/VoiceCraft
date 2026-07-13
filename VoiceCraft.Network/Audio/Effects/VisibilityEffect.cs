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

        [JsonIgnore] public ushort Bitmask { get; set; }

        public event Action<IAudioEffect>? OnDisposed;

        public IAudioEffectProcessor GetProcessor(VoiceCraftEntity entity) =>
            new VisibilityEffectProcessor(this, entity);

        public void Update(IAudioEffect audioEffect)
        {
            if (audioEffect is not VisibilityEffect visibilityEffect)
                throw new ArgumentException("Unexpected Audio Effect Type!", nameof(audioEffect));
            Bitmask = visibilityEffect.Bitmask;
        }

        public void Serialize(NetDataWriter writer)
        {
        }

        public void Deserialize(NetDataReader reader)
        {
        }

        public bool Visibility(VoiceCraftEntity from, VoiceCraftEntity to, ushort effectBitmask)
        {
            var bitmask = from.TalkBitmask & to.ListenBitmask & from.EffectBitmask & to.EffectBitmask;
            if ((effectBitmask & bitmask) == 0) return true; //Disabled, is visible by default.

            return !string.IsNullOrWhiteSpace(from.WorldId) && !string.IsNullOrWhiteSpace(to.WorldId) &&
                   from.WorldId == to.WorldId;
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

    public class VisibilityEffectProcessor : IAudioEffectProcessor
    {
        private bool _disposed;
        public IAudioEffect Effect { get; }
        public VoiceCraftEntity Entity { get; }
        public event Action<IAudioEffectProcessor>? OnDisposed;

        public VisibilityEffectProcessor(VisibilityEffect effect, VoiceCraftEntity entity)
        {
            Effect = effect;
            Entity = entity;
            Effect.OnDisposed += OnEffectDisposed;
        }

        public void Process(VoiceCraftEntity to, Span<float> buffer)
        {
            //Do Nothing
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Effect.OnDisposed -= OnEffectDisposed;
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

        private void OnEffectDisposed(IAudioEffect _)
        {
            Dispose();
        }
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(VisibilityEffect), GenerationMode = JsonSourceGenerationMode.Metadata)]
    public partial class VisibilityEffectGenerationContext : JsonSerializerContext;
}
