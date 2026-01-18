using System;

namespace VoiceCraft.Core.Interfaces
{
    public interface IAudioEncoder: IDisposable
    {
        int Encode(Span<short> data, Span<byte> output, int samples);
        int Encode(Span<float> data, Span<byte> output, int samples);
    }
}