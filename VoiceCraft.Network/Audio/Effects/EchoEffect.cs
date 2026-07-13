using System;
using System.Text.Json.Serialization;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Network.Audio.Effects;

public class EchoEffect : IAudioEffect
{
    public static int SampleRate => Constants.SampleRate;

    public EffectType EffectType => EffectType.Echo;

    [JsonIgnore] public ushort Bitmask { get; set; }

    public event Action<IAudioEffect>? OnDisposed;

    public float Delay
    {
        get => field / SampleRate;
        set => field = SampleRate * Math.Clamp(value, 0.0f, 10.0f);
    }

    public float Feedback
    {
        get;
        set => field = Math.Clamp(value, 0.0f, 1.0f);
    } = 0.5f;

    public float WetDry
    {
        get;
        set => field = Math.Clamp(value, 0.0f, 1.0f);
    } = 1.0f;

    public EchoEffect()
    {
        Delay = 0.5f;
    }

    public IAudioEffectProcessor GetProcessor(VoiceCraftEntity entity) =>
        new EchoEffectProcessor(this, entity);

    public void Update(IAudioEffect audioEffect)
    {
        if (audioEffect is not EchoEffect echoEffect)
            throw new ArgumentException("Unexpected Audio Effect Type!", nameof(audioEffect));
        Bitmask = echoEffect.Bitmask;
        Delay = echoEffect.Delay;
        Feedback = echoEffect.Feedback;
        WetDry = echoEffect.WetDry;
    }
    
    public float EvaluateDelayProperty(VoiceCraftEntity e1, VoiceCraftEntity e2)
    {
        const string property = $"{nameof(EchoEffect)}:{nameof(Delay)}";
        var propVal1 = e1.TryGetProperty<float?>(property, out var prop1);
        var propVal2 = e2.TryGetProperty<float?>(property, out var prop2);
        if (!propVal1 && !propVal2) return Delay;
        return Math.Clamp(Math.Max(prop1 ?? 0.0f, prop2 ?? 0.0f), 0.0f, 10.0f);
    }

    public float EvaluateFeedbackProperty(VoiceCraftEntity e1, VoiceCraftEntity e2)
    {
        const string property = $"{nameof(EchoEffect)}:{nameof(Feedback)}";
        var propVal1 = e1.TryGetProperty<float?>(property, out var prop1);
        var propVal2 = e2.TryGetProperty<float?>(property, out var prop2);
        if (!propVal1 && !propVal2) return Feedback;
        return Math.Clamp(Math.Max(prop1 ?? 0.0f, prop2 ?? 0.0f), 0.0f, 1.0f);
    }

    public float EvaluateWetDryProperty(VoiceCraftEntity e1, VoiceCraftEntity e2)
    {
        const string property = $"{nameof(EchoEffect)}:{nameof(WetDry)}";
        var propVal1 = e1.TryGetProperty<float?>(property, out var prop1);
        var propVal2 = e2.TryGetProperty<float?>(property, out var prop2);
        if (!propVal1 && !propVal2) return WetDry;
        return Math.Clamp(Math.Max(prop1 ?? 0.0f, prop2 ?? 0.0f), 0.0f, 1.0f);
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Delay);
        writer.Put(Feedback);
        writer.Put(WetDry);
    }

    public void Deserialize(NetDataReader reader)
    {
        Delay = reader.GetFloat();
        Feedback = reader.GetFloat();
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

public class EchoEffectProcessor : IAudioEffectProcessor
{
    private readonly EchoEffect _effect;
    private readonly FractionalDelayLine[] _delayLines;
    private bool _disposed;

    public IAudioEffect Effect => _effect;
    public VoiceCraftEntity Entity { get; }
    public event Action<IAudioEffectProcessor>? OnDisposed;

    public EchoEffectProcessor(EchoEffect effect, VoiceCraftEntity entity)
    {
        _effect = effect;
        Entity = entity;
        _delayLines = new FractionalDelayLine[Constants.PlaybackChannels];
        for (var channel = 0; channel < _delayLines.Length; channel++)
            _delayLines[channel] =
                new FractionalDelayLine(Constants.SampleRate, _effect.Delay, InterpolationMode.Nearest);
        Effect.OnDisposed += OnEffectDisposed;
    }

    public void Process(VoiceCraftEntity to, Span<float> buffer)
    {
        var bitmask = Entity.TalkBitmask & to.ListenBitmask & Entity.EffectBitmask & to.EffectBitmask;
        if ((bitmask & Effect.Bitmask) == 0) return;

        //Cache Values
        var wet = _effect.EvaluateWetDryProperty(Entity, to);
        var dry = 1.0f - wet;
        var feedback = _effect.EvaluateFeedbackProperty(Entity, to);
        var delay = _effect.EvaluateDelayProperty(Entity, to);
        foreach (var delayLine in _delayLines)
            delayLine.Ensure(EchoEffect.SampleRate, delay);
        var delaySamples = delay * EchoEffect.SampleRate;

        for (var i = 0; i < buffer.Length; i += Constants.PlaybackChannels)
        {
            for (var channel = 0; channel < Constants.PlaybackChannels && i + channel < buffer.Length; channel++)
            {
                var index = i + channel;
                var delayed = _delayLines[channel].Read(delaySamples);
                var output = buffer[index] + delayed * feedback;
                _delayLines[channel].Write(output);
                buffer[index] = output * wet + buffer[index] * dry;
            }
        }
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
[JsonSerializable(typeof(EchoEffect), GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class EchoEffectGenerationContext : JsonSerializerContext;
