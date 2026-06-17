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
    private readonly FractionalDelayLine _delayLine;

    public IAudioEffect Effect => _effect;
    public VoiceCraftEntity Entity { get; }
    public event Action<IAudioEffectProcessor>? OnDisposed;

    public EchoEffectProcessor(EchoEffect effect, VoiceCraftEntity entity)
    {
        _effect = effect;
        Entity = entity;
        _delayLine = new FractionalDelayLine(Constants.SampleRate, _effect.Delay, InterpolationMode.Nearest);
        Effect.OnDisposed += _ => Dispose();
    }

    public void Process(VoiceCraftEntity to, Span<float> buffer)
    {
        var bitmask = Entity.TalkBitmask & to.ListenBitmask & Entity.EffectBitmask & to.EffectBitmask;
        if ((bitmask & Effect.Bitmask) == 0) return;

        //Cache Values
        var wet = _effect.WetDry;
        var dry = 1.0f - wet;
        var feedback = _effect.Feedback;
        var delay = _effect.Delay;
        _delayLine.Ensure(EchoEffect.SampleRate, delay);
        delay *= EchoEffect.SampleRate;

        for (var i = 0; i < buffer.Length; i++)
        {
            var delayed = _delayLine.Read(delay);
            var output = buffer[i] + delayed * feedback;
            _delayLine.Write(output);
            buffer[i] = output * wet + buffer[i] * dry;
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
[JsonSerializable(typeof(EchoEffect), GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class EchoEffectGenerationContext : JsonSerializerContext;