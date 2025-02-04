using NAudio.Wave;
using System;
using VoiceCraft.Client.Audio.Interfaces;

namespace VoiceCraft.Client.Windows.Audio
{
    public class AudioPlayer : IAudioPlayer
    {
        private bool _disposed;
        private readonly WaveOutEvent _nativePlayer = new();

        public PlaybackState PlaybackState => _nativePlayer.PlaybackState;
        public WaveFormat OutputWaveFormat => _nativePlayer.OutputWaveFormat;

        public float Volume
        {
            get => _nativePlayer.Volume;
            set => _nativePlayer.Volume = value;
        }

        public int DesiredLatency
        {
            get => _nativePlayer.DesiredLatency;
            set => _nativePlayer.DesiredLatency = value;
        }

        public string? SelectedDevice { get; set; }

        public event EventHandler<StoppedEventArgs>? PlaybackStopped;

        public AudioPlayer()
        {
            _nativePlayer.PlaybackStopped += InvokePlaybackStopped;
        }

        ~AudioPlayer()
        {
            //Dispose of this object.
            Dispose(false);
        }

        public void Init(IWaveProvider waveProvider)
        {
            //Disposed? DIE!
            ThrowIfDisposed();

            var selectedDevice = -1;
            for (var n = 0; n < WaveOut.DeviceCount; n++)
            {
                var caps = WaveOut.GetCapabilities(n);
                if (caps.ProductName != SelectedDevice) continue;
                selectedDevice = n;
                break;
            }

            _nativePlayer.DeviceNumber = selectedDevice;
            _nativePlayer.Init(waveProvider);
        }

        public void Play()
        {
            //Disposed? DIE!
            ThrowIfDisposed();

            _nativePlayer.Play();
        }

        public void Pause()
        {
            //Disposed? DIE!
            ThrowIfDisposed();

            _nativePlayer.Pause();
        }

        public void Stop()
        {
            //Disposed? DIE!
            ThrowIfDisposed();

            _nativePlayer.Stop();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }
        
        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(typeof(AudioPlayer).ToString());
        }

        private void InvokePlaybackStopped(object? sender, StoppedEventArgs e)
        {
            PlaybackStopped?.Invoke(sender, e);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing || _disposed) return;

            if (PlaybackState != PlaybackState.Stopped)
                Stop();

            _nativePlayer.PlaybackStopped -= InvokePlaybackStopped;
            _nativePlayer.Dispose();
            _disposed = true;
        }
    }
}