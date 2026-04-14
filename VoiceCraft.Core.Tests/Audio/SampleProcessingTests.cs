using Xunit;
using VoiceCraft.Core.Audio;

namespace VoiceCraft.Core.Tests.Audio;

public class SampleProcessingTests
{
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
