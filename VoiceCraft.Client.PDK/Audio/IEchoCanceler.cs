namespace VoiceCraft.Client.PDK.Audio
{
    public interface IEchoCanceler : IDisposable
    {
        bool IsNative { get; }

        bool IsAvailable { get; }

        bool Enabled { get; }

        bool Initialized { get; }

        void Init(IAudioRecorder recorder);

        void EchoPlayback(byte[] buffer);

        void EchoPlayback(Span<byte> buffer);

        void EchoCancel(byte[] buffer);

        void EchoCancel(Span<byte> buffer);
    }
}
