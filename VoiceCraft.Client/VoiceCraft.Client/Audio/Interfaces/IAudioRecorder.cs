using NAudio.Wave;
using System;
using NAudio.CoreAudioApi;

namespace VoiceCraft.Client.Audio.Interfaces
{
    public interface IAudioRecorder : IDisposable, IWaveIn
    {
        int BufferMilliseconds { get; set; }
        
        CaptureState CaptureState { get; }
        
        string? SelectedDevice { get; set; }
    }
}