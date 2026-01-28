using System;

namespace VoiceCraft.Core.Interfaces;

public interface IClipper
{
    int Read(Span<float> data);
}