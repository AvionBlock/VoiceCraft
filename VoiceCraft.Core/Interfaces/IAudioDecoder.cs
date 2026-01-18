using System;

namespace VoiceCraft.Core.Interfaces
{
    public interface IAudioDecoder : IDisposable
    {
        int Decode(Span<byte> buffer, Span<float> output, int samples);
    }
}