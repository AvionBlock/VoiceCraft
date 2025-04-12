using System;

namespace VoiceCraft.Core.Interfaces
{
    public interface IAudioPlayer : IDisposable
    {
        int SampleRate { get; set; }
        int Channels { get; set; }
        int BitDepth { get; }
        AudioFormat Format { get; set; }
        int BufferMilliseconds { get; set; }
        public string? SelectedDevice { get; set; }
        PlaybackState PlaybackState { get; }
        
        event Action<Exception?>? OnPlaybackStopped;
        
        void Initialize(Func<byte[], int, int, int> playerCallback);
        void Play();
        void Pause();
        void Stop();
    }
}