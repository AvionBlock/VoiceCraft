using VoiceCraft.Client.Audio;
using VoiceCraft.Client.Services;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Tests.Audio;

public class CombinedAudioPreprocessorTests
{
    [Fact]
    public void Process_RunsEnabledProcessorsInOrder()
    {
        var calls = new List<string>();
        using var preprocessor = new CombinedAudioPreprocessor(
            CreateRegistration(Guid.NewGuid(), "gain", calls),
            CreateRegistration(Guid.NewGuid(), "denoiser", calls),
            CreateRegistration(Guid.NewGuid(), "echo", calls));
        var buffer = new float[4];

        preprocessor.Process(buffer);
        preprocessor.ProcessPlayback(buffer);

        Assert.Equal(["gain:process", "denoiser:process", "echo:process", "echo:playback"], calls);
    }

    [Fact]
    public void Constructor_ReusesGainControllerInstanceWhenIdsMatch()
    {
        FakeAudioPreprocessor? sharedInstance = null;
        var sharedId = Guid.NewGuid();
        using var preprocessor = new CombinedAudioPreprocessor(
            new RegisteredAudioPreprocessor(sharedId, "shared", () => sharedInstance = new FakeAudioPreprocessor(), true, true, true),
            CreateRegistration(sharedId, "shared-denoiser"),
            CreateRegistration(sharedId, "shared-echo"));

        preprocessor.Process([0]);
        preprocessor.ProcessPlayback([0]);

        Assert.NotNull(sharedInstance);
        Assert.True(sharedInstance!.GainControllerEnabled);
        Assert.True(sharedInstance.DenoiserEnabled);
        Assert.True(sharedInstance.EchoCancelerEnabled);
    }

    [Fact]
    public void Dispose_DisposesInstantiatedProcessorsAndBlocksFurtherUse()
    {
        FakeAudioPreprocessor? gain = null;
        FakeAudioPreprocessor? denoiser = null;
        var preprocessor = new CombinedAudioPreprocessor(
            new RegisteredAudioPreprocessor(Guid.NewGuid(), "gain", () => gain = new FakeAudioPreprocessor(), true, true, true),
            new RegisteredAudioPreprocessor(Guid.NewGuid(), "denoiser", () => denoiser = new FakeAudioPreprocessor(), true, true, true),
            null);
        
        preprocessor.Dispose();
        Assert.NotNull(gain);
        Assert.NotNull(denoiser);
        Assert.Equal(1, gain.DisposeCalls);
        Assert.Equal(1, denoiser.DisposeCalls);
        Assert.Throws<ObjectDisposedException>(() => preprocessor.Process([0]));
    }

    private static RegisteredAudioPreprocessor CreateRegistration(Guid id, string name, List<string>? calls = null)
    {
        return new RegisteredAudioPreprocessor(id, name, () => new FakeAudioPreprocessor(calls, name), true, true, true);
    }

    private sealed class FakeAudioPreprocessor(List<string>? calls = null, string? name = null) : IAudioPreprocessor
    {
        public bool DenoiserEnabled { get; set; }
        public bool GainControllerEnabled { get; set; }
        public bool EchoCancelerEnabled { get; set; }
        public int TargetGain { get; set; }
        public int DisposeCalls { get; private set; }

        public void Process(Span<float> buffer)
        {
            calls?.Add($"{name}:process");
        }

        public void ProcessPlayback(Span<float> buffer)
        {
            calls?.Add($"{name}:playback");
        }

        public void Dispose()
        {
            DisposeCalls++;
        }
    }
}
