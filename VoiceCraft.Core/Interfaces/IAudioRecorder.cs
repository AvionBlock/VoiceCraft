using System;

namespace VoiceCraft.Core.Interfaces
{
    public interface IAudioRecorder : IDisposable
    {
        int SampleRate { get; set; }
        int Channels { get; set; }
        int BitDepth { get; }
        AudioFormat Format { get; set; }
        int BufferMilliseconds { get; set; }
        string? SelectedDevice { get; set; }
        CaptureState CaptureState { get; }

        event Action<byte[], int>? OnDataAvailable;
        event Action<Exception?>? OnRecordingStopped;

        void Initialize();
        void Start();
        void Stop();
    }
}