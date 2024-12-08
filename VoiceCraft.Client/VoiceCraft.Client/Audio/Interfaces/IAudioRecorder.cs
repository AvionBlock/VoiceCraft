using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace VoiceCraft.Client.Audio.Interfaces
{
    public interface IAudioRecorder : IWaveIn
    {
        int BufferMilliseconds { get; set; }
        
        CaptureState CaptureState { get; }
        
        string? SelectedDevice { get; set; }
    }
}