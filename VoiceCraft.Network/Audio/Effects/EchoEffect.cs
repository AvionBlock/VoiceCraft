using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Network.Audio.Effects;

public class EchoEffect : IAudioEffect
{
    private readonly Dictionary<VoiceCraftEntity, FractionalDelayLine> _delayLines = new();

    private float _delay;
    private float _wetDry = 1.0f;
    private float _feedback = 0.5f;

    public EchoEffect()
    {
        Delay = 0.5f;
    }

    public float WetDry
    {
        get => _wetDry;
        set => _wetDry = Math.Clamp(value, 0.0f, 1.0f);
    }

    public static int SampleRate => Constants.SampleRate;

    public float Delay
    {
        get => _delay / SampleRate;
        set => _delay = SampleRate * Math.Clamp(value, 0.0f, 10.0f);
    }

    public float Feedback
    {
        get => _feedback;
        set => _feedback = Math.Clamp(value, 0.0f, 1.0f);
    }

    public EffectType EffectType => EffectType.Echo;

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

    public void Process(VoiceCraftEntity from, VoiceCraftEntity to, ushort effectBitmask, Span<float> buffer)
    {
        var bitmask = from.TalkBitmask & to.ListenBitmask & from.EffectBitmask & to.EffectBitmask;
        if ((bitmask & effectBitmask) == 0)
            return; //There may still be echo from the entity itself but that will phase out over time.

        var delayLine = GetOrCreateDelayLine(from);
        delayLine.Ensure(SampleRate, Delay);

        for (var i = 0; i < buffer.Length; i++)
        {
            var delayed = delayLine.Read(_delay);
            var output = buffer[i] + delayed * Feedback;
            delayLine.Write(output);
            buffer[i] = output * WetDry + buffer[i] * (1.0f - WetDry);
        }
    }

    public void Reset()
    {
        lock (_delayLines)
        {
            _delayLines.Clear();
        }
    }

    public void Dispose()
    {
        //Nothing to dispose.
    }

    private FractionalDelayLine GetOrCreateDelayLine(VoiceCraftEntity entity)
    {
        lock (_delayLines)
        {
            if (_delayLines.TryGetValue(entity, out var delayLine))
                return delayLine;
            delayLine = new FractionalDelayLine(SampleRate, Delay, InterpolationMode.Nearest);
            _delayLines.TryAdd(entity, delayLine);
            entity.OnDestroyed += RemoveDelayLine;
            return delayLine;
        }
    }

    private void RemoveDelayLine(VoiceCraftEntity entity)
    {
        lock (_delayLines)
        {
            _delayLines.Remove(entity);
            entity.OnDestroyed -= RemoveDelayLine;
        }
    }
}