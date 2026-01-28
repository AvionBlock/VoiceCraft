using System;

namespace VoiceCraft.Core.Interfaces;

public interface IAudioClipper
{
    int Read(Span<float> data);
}