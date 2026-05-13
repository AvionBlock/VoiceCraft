using System;
using System.Text.Json.Serialization;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Network.Audio.Effects;

public class EchoEffect : IAudioEffect
{
    public static int SampleRate => Constants.SampleRate;

    public EffectType EffectType => EffectType.Echo;
    
    [JsonIgnore]
    public ushort Bitmask { get; set; }
    
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
    
    public void Update(IAudioEffect audioEffect)
    {
        if(audioEffect is not EchoEffect echoEffect)
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

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(EchoEffect), GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class EchoEffectGenerationContext : JsonSerializerContext;