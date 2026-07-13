using System.Reflection;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Network.World;
using Xunit;

namespace VoiceCraft.Network.Tests.World;

public class VoiceCraftClientEntityTests
{
    [Fact]
    public void Read_ConvertsOnlySamplesActuallyReadFromMonoBuffer()
    {
        var entity = new VoiceCraftClientEntity(1, new FakeAudioDecoder());
        try
        {
            var outputBuffer = GetOutputBuffer(entity);
            outputBuffer.PrefillSize = 1;
            outputBuffer.Write([0.25f]);
            var output = Enumerable.Repeat(-1f, 6).ToArray();

            var read = entity.Read(output);

            Assert.Equal(2, read);
            Assert.Equal(0.25f, output[0], 5);
            Assert.Equal(0.25f, output[1], 5);
            Assert.All(output[2..], sample => Assert.Equal(-1f, sample));
        }
        finally
        {
            entity.Destroy();
        }
    }

    [Fact]
    public void OutputBuffer_UsesMonoSampleCountsForCapacityAndPrefill()
    {
        var entity = new VoiceCraftClientEntity(1, new FakeAudioDecoder());
        try
        {
            var outputBuffer = GetOutputBuffer(entity);

            Assert.Equal(Constants.OutputBufferSize, outputBuffer.MaxLength);
            Assert.Equal(Constants.PrefillBufferSize, outputBuffer.PrefillSize);
        }
        finally
        {
            entity.Destroy();
        }
    }

    [Fact]
    public void Destroy_IsIdempotentForDecoder()
    {
        var decoder = new FakeAudioDecoder();
        var entity = new VoiceCraftClientEntity(1, decoder);

        entity.Destroy();
        entity.Destroy();

        Assert.Equal(1, decoder.DisposeCount);
    }

    private static SampleBufferProvider<float> GetOutputBuffer(VoiceCraftClientEntity entity)
    {
        var field = typeof(VoiceCraftClientEntity).GetField("_outputBuffer",
            BindingFlags.Instance | BindingFlags.NonPublic);
        return Assert.IsType<SampleBufferProvider<float>>(field?.GetValue(entity));
    }

    private sealed class FakeAudioDecoder : IAudioDecoder
    {
        public int DisposeCount { get; private set; }

        public int Decode(Span<byte> buffer, Span<float> output, int samples)
        {
            return 0;
        }

        public void Dispose()
        {
            DisposeCount++;
        }
    }
}
