using System;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.iOS;

public class IosAudioPreprocessor : IAudioPreprocessor
{
    public bool DenoiserEnabled { get; set; } = true;
    public bool GainControllerEnabled { get; set; } = true;
    public bool EchoCancelerEnabled { get; set; } = true;
    public int TargetGain { get; set; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public void Process(Span<float> buffer)
    {
    }

    public void ProcessPlayback(Span<float> buffer)
    {
    }
}
