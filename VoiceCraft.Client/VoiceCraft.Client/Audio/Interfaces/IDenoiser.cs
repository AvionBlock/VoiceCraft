using System;

namespace VoiceCraft.Client.Audio.Interfaces
{
    public interface IDenoiser : IDisposable
    {
        bool IsNative { get; }
        
        void Init(IAudioRecorder recorder);

        void Denoise(byte[] buffer);
        
        void Denoise(Span<byte> buffer);
    }
}