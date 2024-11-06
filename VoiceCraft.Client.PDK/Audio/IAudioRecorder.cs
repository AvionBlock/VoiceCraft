using NAudio.Wave;

namespace VoiceCraft.Client.PDK.Audio
{
    public interface IAudioRecorder : IDisposable, IWaveIn
    {
        int BufferMilliseconds { get; set; }

        bool IsRecording { get; }

        void SetDevice(string device);
    }
}
