using NAudio.Wave;
using System;
using System.Threading;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core;
using PlaybackState = VoiceCraft.Core.PlaybackState;

namespace VoiceCraft.Client.Windows.Audio
{
    public class AudioPlayer : IAudioPlayer
    {
        //Public Properties
        public int SampleRate
        {
            get => _sampleRate;
            set
            {
                if(value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Sample rate must be greater than or equal to zero!");

                _sampleRate = value;
            }
        }

        public int Channels
        {
            get => _channels;
            set
            {
                if(value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Channels must be greater than or equal to one!");

                _channels = value;
            }
        }
        
        public int BitDepth
        {
            get
            {
                return (Format) switch
                {
                    AudioFormat.Pcm8 => 8,
                    AudioFormat.Pcm16 => 16,
                    AudioFormat.PcmFloat => 32,
                    _ => throw new ArgumentOutOfRangeException(nameof(Format))
                };
            }
        }

        public AudioFormat Format { get; set; }

        public int BufferMilliseconds
        {
            get => _bufferMilliseconds;
            set
            {
                if(value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Buffer milliseconds must be greater than or equal to zero!");

                _bufferMilliseconds = value;
            }
        }

        public string? SelectedDevice { get; set; }

        public PlaybackState PlaybackState { get; private set; }

        public event Action<Exception?>? OnPlaybackStopped;
        
        //Privates
        private readonly SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;
        private WaveOutEvent? _nativePlayer;
        private int _sampleRate;
        private int _channels;
        private int _bufferMilliseconds;
        private bool _disposed;
        
        public AudioPlayer(int sampleRate, int channels, AudioFormat format)
        {
            SampleRate = sampleRate;
            Channels = channels;
            Format = format;
        }

        ~AudioPlayer()
        {
            //Dispose of this object.
            Dispose(false);
        }

        public void Initialize(Func<byte[], int, int, int> playerCallback)
        {
            //Disposed? DIE!
            ThrowIfDisposed(); 
            
            if(PlaybackState != PlaybackState.Stopped)
                throw new InvalidOperationException("Cannot initialize when playing!");
            
            //Cleanup previous player.
            CleanupPlayer();

            try
            {
                //Select Device.
                var selectedDevice = -1;
                for (var n = 0; n < WaveOut.DeviceCount; n++)
                {
                    var caps = WaveOut.GetCapabilities(n);
                    if (caps.ProductName != SelectedDevice) continue;
                    selectedDevice = n;
                    break;
                }

                //Setup WaveFormat
                var waveFormat = Format switch
                {
                    AudioFormat.Pcm8 => new WaveFormat(SampleRate, 8, Channels),
                    AudioFormat.Pcm16 => new WaveFormat(SampleRate, 16, Channels),
                    AudioFormat.PcmFloat => WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, Channels),
                    _ => throw new NotSupportedException("Input format is not supported!")
                };
                
                var callbackProvider = new CallbackWaveProvider(waveFormat, playerCallback);
                
                //Setup Player
                _nativePlayer = new WaveOutEvent();
                _nativePlayer.DesiredLatency = BufferMilliseconds;
                _nativePlayer.DeviceNumber = selectedDevice;
                _nativePlayer.Volume = 1.0f;
                _nativePlayer.NumberOfBuffers = 3;
                _nativePlayer.Init(callbackProvider);
            }
            catch
            {
                CleanupPlayer();
                throw;
            }
        }
        
        public void Play()
        {
            //Disposed? DIE!
            ThrowIfDisposed();
            ThrowIfNotInitialized();
            if (PlaybackState != PlaybackState.Stopped) return;

            try
            {
                PlaybackState = PlaybackState.Starting;
                _nativePlayer?.Play();
                PlaybackState = PlaybackState.Playing;
            }
            catch
            {
                PlaybackState = PlaybackState.Stopped;
                throw;
            }
        }

        public void Pause()
        {
            //Disposed? DIE!
            ThrowIfDisposed();
            ThrowIfNotInitialized();
            if (PlaybackState != PlaybackState.Playing) return;
            
            PlaybackState = PlaybackState.Paused;
            _nativePlayer?.Pause();
        }

        public void Stop()
        {
            //Disposed? DIE!
            ThrowIfDisposed();
            ThrowIfNotInitialized();
            if (PlaybackState is not (PlaybackState.Playing or PlaybackState.Paused)) return;
            
            PlaybackState = PlaybackState.Stopping;
            _nativePlayer?.Stop();

            while (PlaybackState == PlaybackState.Stopping)
            {
                Thread.Sleep(1); //Wait until stopped.
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void CleanupPlayer()
        {
            if (_nativePlayer == null) return;
            _nativePlayer.PlaybackStopped -= InvokePlaybackStopped;
            _nativePlayer.Dispose();
            _nativePlayer = null;
        }
        
        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(typeof(AudioPlayer).ToString());
        }

        private void ThrowIfNotInitialized()
        {
            if(_nativePlayer == null)
                throw new InvalidOperationException("Audio player is not intialized!");
        }
        
        private void InvokePlaybackStopped(object? sender, StoppedEventArgs e)
        {
            PlaybackState = PlaybackState.Stopped;
            var handler = OnPlaybackStopped;
            if (handler == null) return;
            if (_synchronizationContext == null)
            {
                handler(e.Exception);
            }
            else
            {
                _synchronizationContext.Post(_ => handler(e.Exception), null);
            }
        }
        
        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                CleanupPlayer();
            }
            
            _disposed = true;
        }

        private class CallbackWaveProvider(WaveFormat waveFormat, Func<byte[], int, int, int> callback) : IWaveProvider
        {
            public WaveFormat WaveFormat { get; } = waveFormat;

            public int Read(byte[] buffer, int offset, int count)
            {
                return callback(buffer, offset, count);
            }
        }
    }
}