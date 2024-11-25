namespace VoiceCraft.Client.PDK.Audio
{
    public interface IEchoCanceller : IDisposable
    {
        void Init(IAudioRecorder recorder);

        void EchoPlayback(byte[] buffer);

        void EchoPlayback(Span<byte> buffer);

        void EchoCancel(byte[] buffer);

        void EchoCancel(Span<byte> buffer);
    }
}
