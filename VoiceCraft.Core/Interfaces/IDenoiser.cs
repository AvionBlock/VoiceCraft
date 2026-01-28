using System;

namespace VoiceCraft.Core.Interfaces
{
    public interface IDenoiser : IDisposable
    {
        bool IsNative { get; }

        void Initialize(IAudioRecorder recorder);

        void Denoise(Span<byte> buffer);
    }
}