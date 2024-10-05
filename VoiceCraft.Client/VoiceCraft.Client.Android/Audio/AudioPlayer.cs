using NAudio.Wave;
using System;
using VoiceCraft.Client.PDK.Audio;

namespace VoiceCraft.Client.Android.Audio
{
    public class AudioPlayer : IAudioPlayer
    {
        public int DesiredLatency { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public float Volume { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public PlaybackState PlaybackState => throw new NotImplementedException();

        public WaveFormat OutputWaveFormat => throw new NotImplementedException();

        public event EventHandler<StoppedEventArgs>? PlaybackStopped;

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Init(IWaveProvider waveProvider)
        {
            throw new NotImplementedException();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Play()
        {
            throw new NotImplementedException();
        }

        public void SetDevice(string device)
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
