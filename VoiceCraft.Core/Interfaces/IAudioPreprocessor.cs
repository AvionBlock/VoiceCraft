using System;

namespace VoiceCraft.Core.Interfaces;

public interface IAudioPreprocessor : IDisposable
{
    bool DenoiserEnabled { get; set; }
    bool GainControllerEnabled { get; set; }
    bool EchoCancelerEnabled { get; set; }
    int TargetGain { get; set; }
    
    void Process(Span<float> buffer);
    void ProcessPlayback(Span<float> buffer);
}