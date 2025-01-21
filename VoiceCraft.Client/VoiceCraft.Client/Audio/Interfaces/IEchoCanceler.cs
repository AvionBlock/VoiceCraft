using System;

namespace VoiceCraft.Client.Audio.Interfaces
{
    public interface IEchoCanceler : IDisposable
    {
        bool IsNative { get; }
        
        void Init(IAudioRecorder recorder, IAudioPlayer player);

        void EchoPlayback(byte[] buffer);

        void EchoPlayback(Span<byte> buffer);

        void EchoCancel(byte[] buffer);

        void EchoCancel(Span<byte> buffer);
    }
}