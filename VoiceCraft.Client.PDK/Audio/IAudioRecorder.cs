using NAudio.Wave;

namespace VoiceCraft.Client.PDK.Audio
{
    public interface IAudioRecorder : IDisposable, IWaveIn
    {
        public static readonly WaveFormat RecordFormat = new WaveFormat(48000, 1);
        public static readonly int BufferMilliseconds = 20;

        IWaveIn NativeRecorder { get; }
        bool IsRecording { get; }

        void SetDevice(string device);
    }
}
