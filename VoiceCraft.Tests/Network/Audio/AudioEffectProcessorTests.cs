using System;
using System.Numerics;
using VoiceCraft.Core;
using VoiceCraft.Core.World;
using VoiceCraft.Network;
using VoiceCraft.Network.Audio.Effects;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Tests.Network.Audio;

public class AudioEffectProcessorTests
{
    [Fact]
    public void EchoEffect_KeepsStereoChannelHistoryIndependent()
    {
        var from = new VoiceCraftEntity(1);
        var to = new VoiceCraftEntity(2);
        using var effect = new EchoEffect();
        effect.Bitmask = 1;
        effect.Delay = 1f / Constants.SampleRate;
        effect.Feedback = 1f;
        effect.WetDry = 1f;
        
        using var processor = effect.GetProcessor(from);
        Span<float> buffer = [1f, 0f, 0f, 0f];

        processor.Process(to, buffer);

        Assert.Equal(1f, buffer[0], 5);
        Assert.Equal(0f, buffer[1], 5);
        Assert.Equal(1f, buffer[2], 5);
        Assert.Equal(0f, buffer[3], 5);
    }

    [Fact]
    public void MuffleEffect_ProcessesIdenticalStereoChannelsIdentically()
    {
        var from = new VoiceCraftEntity(1);
        var to = new VoiceCraftEntity(2);
        using var effect = new MuffleEffect();
        effect.Bitmask = 1;
        effect.WetDry = 1f;
        
        using var processor = effect.GetProcessor(from);
        Span<float> buffer = [1f, 1f, 0f, 0f, 0f, 0f];

        processor.Process(to, buffer);

        for (var i = 0; i < buffer.Length; i += Constants.PlaybackChannels)
            Assert.Equal(buffer[i], buffer[i + 1], 6);
    }

    [Fact]
    public void ProximityEffect_AdvancesFadeOncePerStereoFrame()
    {
        var from = new VoiceCraftEntity(1);
        var to = new VoiceCraftEntity(2) { Position = new Vector3(10, 0, 0) };
        using var effect = new ProximityEffect();
        effect.Bitmask = 1;
        effect.MinRange = 0;
        effect.MaxRange = 10;
        effect.WetDry = 1f;
        
        using var processor = effect.GetProcessor(from);
        Span<float> buffer = [1f, 1f, 1f, 1f];

        processor.Process(to, buffer);

        Assert.Equal(buffer[0], buffer[1], 6);
        Assert.Equal(buffer[2], buffer[3], 6);
        Assert.True(buffer[2] < buffer[0]);
    }

    [Theory]
    [InlineData(EffectType.Visibility)]
    [InlineData(EffectType.Proximity)]
    [InlineData(EffectType.Directional)]
    [InlineData(EffectType.ProximityEcho)]
    [InlineData(EffectType.Echo)]
    [InlineData(EffectType.ProximityMuffle)]
    [InlineData(EffectType.Muffle)]
    public void ProcessorDispose_UnsubscribesFromEffectAndIsIdempotent(EffectType effectType)
    {
        var effect = CreateEffect(effectType);
        var processor = effect.GetProcessor(new VoiceCraftEntity(1));
        var disposedCount = 0;
        processor.OnDisposed += _ => disposedCount++;

        processor.Dispose();
        effect.Dispose();

        Assert.Equal(1, disposedCount);
    }

    [Theory]
    [InlineData(EffectType.Visibility)]
    [InlineData(EffectType.Proximity)]
    [InlineData(EffectType.Directional)]
    [InlineData(EffectType.ProximityEcho)]
    [InlineData(EffectType.Echo)]
    [InlineData(EffectType.ProximityMuffle)]
    [InlineData(EffectType.Muffle)]
    public void Processor_DoesNotChangeAudioWhenEffectIsMaskedOff(EffectType effectType)
    {
        var source = new VoiceCraftEntity(1) { TalkBitmask = 0 };
        var target = new VoiceCraftEntity(2);
        using var effect = CreateEffect(effectType);
        effect.Bitmask = 1;
        using var processor = effect.GetProcessor(source);
        float[] buffer = [1f, -0.5f, 0.25f, 0f];

        processor.Process(target, buffer);

        Assert.Equal([1f, -0.5f, 0.25f, 0f], buffer);
    }

    [Theory]
    [InlineData(EffectType.Visibility)]
    [InlineData(EffectType.Proximity)]
    [InlineData(EffectType.Directional)]
    [InlineData(EffectType.ProximityEcho)]
    [InlineData(EffectType.Echo)]
    [InlineData(EffectType.ProximityMuffle)]
    [InlineData(EffectType.Muffle)]
    public void DisposingEffect_DisposesItsProcessor(EffectType effectType)
    {
        var effect = CreateEffect(effectType);
        var processor = effect.GetProcessor(new VoiceCraftEntity(1));
        var disposedCount = 0;
        processor.OnDisposed += _ => disposedCount++;

        effect.Dispose();
        effect.Dispose();

        Assert.Equal(1, disposedCount);
    }

    private static IAudioEffect CreateEffect(EffectType effectType)
    {
        return effectType switch
        {
            EffectType.Visibility => new VisibilityEffect(),
            EffectType.Proximity => new ProximityEffect(),
            EffectType.Directional => new DirectionalEffect(),
            EffectType.ProximityEcho => new ProximityEchoEffect(),
            EffectType.Echo => new EchoEffect(),
            EffectType.ProximityMuffle => new ProximityMuffleEffect(),
            EffectType.Muffle => new MuffleEffect(),
            _ => throw new ArgumentOutOfRangeException(nameof(effectType), effectType, null)
        };
    }
}
