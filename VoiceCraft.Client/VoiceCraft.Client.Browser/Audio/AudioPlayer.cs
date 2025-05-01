using System;
using System.Threading;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Browser.Audio
{
    public class AudioPlayer : IAudioPlayer
    {
        private const int NumberOfBuffers = 3;

        //Public Properties
        public int SampleRate
        {
            get => _sampleRate;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Sample rate must be greater than or equal to zero!");

                _sampleRate = value;
            }
        }

        public int Channels
        {
            get => _channels;
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Channels must be greater than or equal to one!");

                _channels = value;
            }
        }

        public int BitDepth
        {
            get
            {
                return Format switch
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
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Buffer milliseconds must be greater than or equal to zero!");

                _bufferMilliseconds = value;
            }
        }

        public string? SelectedDevice { get; set; }

        public PlaybackState PlaybackState { get; private set; }

        public event Action<Exception?>? OnPlaybackStopped;

        private readonly Lock _lockObj = new();
        private readonly SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;
        private Func<byte[], int, int, int>? _playerCallback;
        private int _bufferBytes;
        private bool _disposed;

        private int _sampleRate;
        private int _channels;
        private int _bufferMilliseconds;

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
            _lockObj.Enter();

            try
            {
                //Disposed? DIE!
                ThrowIfDisposed();

                //Check if already playing.
                if (PlaybackState != PlaybackState.Stopped)
                    throw new InvalidOperationException(Locales.Locales.Audio_Player_InitFailed);

                //Cleanup previous player.
                CleanupPlayer();

                _playerCallback = playerCallback;
                
                var blockAlign = Channels * (BitDepth / 8);
                var bytesPerSecond = _sampleRate * blockAlign;
                var bufferMs = (BufferMilliseconds + NumberOfBuffers - 1) / NumberOfBuffers;
                _bufferBytes = (int) (bytesPerSecond / 1000.0 * bufferMs);
                if (_bufferBytes % blockAlign != 0)
                {
                    _bufferBytes = _bufferBytes + blockAlign - _bufferBytes % blockAlign;
                }
            }
            catch
            {
                CleanupPlayer();
                throw;
            }
            finally
            {
                _lockObj.Exit();
            }
        }

        public void Play()
        {
            _lockObj.Enter();

            try
            {
                //Disposed? DIE!
                ThrowIfDisposed();
                ThrowIfNotInitialized();

                //Resume or start playback.
                switch (PlaybackState)
                {
                    case PlaybackState.Stopped:
                        PlaybackState = PlaybackState.Starting;
                        ThreadPool.QueueUserWorkItem(_ => PlaybackThread(), null);
                        break;
                    case PlaybackState.Paused:
                        Resume();
                        break;
                    case PlaybackState.Starting:
                    case PlaybackState.Playing:
                    case PlaybackState.Stopping:
                    default:
                        break;
                }
            }
            catch
            {
                PlaybackState = PlaybackState.Stopped;
                throw;
            }
            finally
            {
                _lockObj.Exit();
            }
        }

        public void Pause()
        {
            _lockObj.Enter();

            try
            {
                //Disposed? DIE!
                ThrowIfDisposed();
                ThrowIfNotInitialized();
                if (PlaybackState != PlaybackState.Playing) return;
                
                PlaybackState = PlaybackState.Paused;
            }
            finally
            {
                _lockObj.Exit();
            }
        }

        public void Stop()
        {
            _lockObj.Enter();

            try
            {
                //Disposed? DIE!
                ThrowIfDisposed();
                ThrowIfNotInitialized();
                
                if (PlaybackState != PlaybackState.Playing) return;

                PlaybackState = PlaybackState.Stopping;
            }
            finally
            {
                _lockObj.Exit();
            }
        }

        public void Dispose()
        {
            _lockObj.Enter();

            try
            {
                //Dispose of this object
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            finally
            {
                _lockObj.Exit();
            }
        }

        private void CleanupPlayer()
        {
        }

        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(typeof(AudioPlayer).ToString());
        }

        private void ThrowIfNotInitialized()
        {
            if (_playerCallback == null)
                throw new InvalidOperationException(Locales.Locales.Audio_Player_Init);
        }

        private void Resume()
        {
            if (PlaybackState != PlaybackState.Paused) return;
            PlaybackState = PlaybackState.Playing;
        }

        private void InvokePlaybackStopped(Exception? exception = null)
        {
            PlaybackState = PlaybackState.Stopped;
            var handler = OnPlaybackStopped;
            if (handler == null) return;
            if (_synchronizationContext == null)
            {
                handler(exception);
            }
            else
            {
                _synchronizationContext.Post(_ => handler(exception), null);
            }
        }

        private void PlaybackThread()
        {
            Exception? exception = null;
            try
            {
                PlaybackLogic();
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                InvokePlaybackStopped(exception);
            }
        }

        private void Dispose(bool _)
        {
            if (_disposed) return;

            //Unmanaged resource. cleanup is necessary.
            CleanupPlayer();

            _disposed = true;
        }

        private void PlaybackLogic()
        {
            //This shouldn't happen...
            if (_playerCallback == null)
                throw new InvalidOperationException();
        }
    }
}