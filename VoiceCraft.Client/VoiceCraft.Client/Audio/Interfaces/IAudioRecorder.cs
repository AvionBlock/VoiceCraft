using NAudio.Wave;
using System;

namespace VoiceCraft.Client.Audio.Interfaces
{
    public interface IAudioRecorder : IDisposable, IWaveIn
    {
        int BufferMilliseconds { get; set; }

        bool IsRecording { get; }

        void SetDevice(string device);
    }
}