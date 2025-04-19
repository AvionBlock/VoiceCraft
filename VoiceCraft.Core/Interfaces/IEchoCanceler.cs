using System;

namespace VoiceCraft.Core.Interfaces
{
    public interface IEchoCanceler : IDisposable
    {
        bool IsNative { get; }
        
        void Initialize(IAudioRecorder recorder, IAudioPlayer player);

        void EchoPlayback(byte[] buffer, int count);

        void EchoPlayback(Span<byte> buffer, int count);

        void EchoCancel(byte[] buffer, int count);

        void EchoCancel(Span<byte> buffer, int count);
    }
}