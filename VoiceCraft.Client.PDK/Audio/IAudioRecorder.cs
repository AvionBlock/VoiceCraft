using NAudio.Wave;

namespace VoiceCraft.Client.PDK.Audio
{
    public interface IAudioRecorder : IDisposable, IWaveIn
    {
        IWaveIn NativeRecorder { get; }
        bool IsRecording { get; }

        void SetDevice(string device);
    }
}
