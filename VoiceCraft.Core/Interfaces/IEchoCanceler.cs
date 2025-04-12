using System;

namespace VoiceCraft.Core.Interfaces
{
    public interface IEchoCanceler : IDisposable
    {
        bool IsNative { get; }
        
        void Initialize(IAudioRecorder recorder, IAudioPlayer player);

        void EchoPlayback(byte[] buffer);

        void EchoPlayback(Span<byte> buffer);

        void EchoCancel(byte[] buffer);

        void EchoCancel(Span<byte> buffer);
    }
}