using Xunit;
using VoiceCraft.Core.Audio;

namespace VoiceCraft.Core.Tests.Audio;

public class SampleProcessingTests
{
    [Fact]
    public void SampleLerpVolume_DefaultTarget_StartsAtUnity()
    {
        var volume = new SampleLerpVolume(10, TimeSpan.FromSeconds(1));

        var transformed = volume.Transform(0.5f);

        Assert.Equal(0.5f, transformed, 3);
    }

    [Fact]
    public void SampleLerpVolume_ZeroDuration_AppliesTargetImmediately()
    {
        var volume = new SampleLerpVolume(48_000, TimeSpan.Zero)
        {
            TargetVolume = 0.5f
        };

        var transformed = volume.Transform(1f);

        Assert.True(float.IsFinite(transformed));
        Assert.Equal(0.5f, transformed, 3);
    }

    [Fact]
    public void SampleLerpVolume_CompletedFade_DoesNotOvershootTarget()
    {
        var volume = new SampleLerpVolume(2, TimeSpan.FromSeconds(1))
        {
            TargetVolume = 0f
        };

        Assert.Equal(1f, volume.Transform(1f), 3);
        volume.Step();
        Assert.Equal(0.5f, volume.Transform(1f), 3);
        volume.Step();
        Assert.Equal(0f, volume.Transform(1f), 3);
        volume.Step();

        Assert.Equal(0f, volume.Transform(1f), 3);
    }

    [Fact]
    public void SampleLerpVolume_Retargeting_DoesNotJumpFromCurrentVolume()
    {
        var volume = new SampleLerpVolume(4, TimeSpan.FromSeconds(1))
        {
            TargetVolume = 0f
        };

        volume.Step();
        Assert.Equal(0.75f, volume.Transform(1f), 3);

        volume.TargetVolume = 1f;

        Assert.Equal(0.75f, volume.Transform(1f), 3);
    }

    [Fact]
    public void SampleBufferProvider_ExactDrain_ReappliesPrefillThreshold()
    {
        var provider = new SampleBufferProvider<int>(4) { PrefillSize = 2 };
        provider.Write([1, 2]);
        Span<int> firstRead = stackalloc int[2];
        Assert.Equal(2, provider.Read(firstRead));

        provider.Write([3]);
        Span<int> secondRead = stackalloc int[1];

        Assert.Equal(0, provider.Read(secondRead));
        Assert.Equal(1, provider.Count);
    }

    [Fact]
    public void SampleBufferProvider_Reset_ReappliesPrefillThreshold()
    {
        var provider = new SampleBufferProvider<int>(4) { PrefillSize = 2 };
        provider.Write([1, 2]);
        Span<int> firstRead = stackalloc int[1];
        Assert.Equal(1, provider.Read(firstRead));

        provider.Reset();
        provider.Write([3]);
        Span<int> secondRead = stackalloc int[1];

        Assert.Equal(0, provider.Read(secondRead));
        Assert.Equal(1, provider.Count);
    }

    [Fact]
    public void FractionalDelayLine_Ensure_PreservesAvailableHistoryByDelay()
    {
        var delayLine = new FractionalDelayLine(4, InterpolationMode.Nearest);
        delayLine.Write(1f);
        delayLine.Write(2f);
        delayLine.Write(3f);
        delayLine.Write(4f);

        delayLine.Ensure(6);

        Assert.Equal(4f, delayLine.Read(1));
        Assert.Equal(3f, delayLine.Read(2));
        Assert.Equal(2f, delayLine.Read(3));
        Assert.Equal(0f, delayLine.Read(4));
        Assert.Equal(0f, delayLine.Read(5));
    }

    [Fact]
    public void SampleVolume_Read_ScalesSamplesAndClampsVolume()
    {
        Span<float> samples = [0.25f, -0.5f, 1.0f];

        var read = SampleVolume.Read(samples, 3.0f);

        Assert.Equal(samples.Length, read);
        Assert.Equal(0.5f, samples[0], 3);
        Assert.Equal(-1.0f, samples[1], 3);
        Assert.Equal(2.0f, samples[2], 3);
    }

    [Fact]
    public void SampleLoudness_Read_ReturnsMaximumAbsoluteSample()
    {
        Span<float> samples = [-0.25f, 0.75f, -0.5f];

        var loudness = SampleLoudness.Read(samples);

        Assert.Equal(0.75f, loudness, 3);
    }

    [Fact]
    public void SampleMonoToStereo_Read_DuplicatesEachSample()
    {
        Span<float> mono = [0.1f, -0.2f, 0.3f];
        Span<float> stereo = stackalloc float[mono.Length * 2];

        var written = SampleMonoToStereo.Read(mono, stereo);

        Assert.Equal(6, written);
        Assert.Equal([0.1f, 0.1f, -0.2f, -0.2f, 0.3f, 0.3f], stereo.ToArray());
    }

    [Fact]
    public void Sample16ToFloat_Read_ConvertsPcm16ToNormalizedFloat()
    {
        Span<short> pcm16 = [short.MinValue, 0, short.MaxValue];
        Span<float> floats = stackalloc float[pcm16.Length];

        var read = Sample16ToFloat.Read(pcm16, floats);

        Assert.Equal(3, read);
        Assert.Equal(-1.0f, floats[0], 3);
        Assert.Equal(0.0f, floats[1], 3);
        Assert.InRange(floats[2], 0.9999f, 1.0f);
    }

    [Fact]
    public void SampleFloatTo16_Read_ClampsOutOfRangeValues()
    {
        Span<float> floats = [-2.0f, -0.5f, 0.5f, 2.0f];
        Span<short> pcm16 = stackalloc short[floats.Length];

        var read = SampleFloatTo16.Read(floats, pcm16);

        Assert.Equal(4, read);
        Assert.Equal(short.MinValue, pcm16[0]);
        Assert.Equal((short)-16383, pcm16[1]);
        Assert.Equal((short)16383, pcm16[2]);
        Assert.Equal(short.MaxValue, pcm16[3]);
    }
}
