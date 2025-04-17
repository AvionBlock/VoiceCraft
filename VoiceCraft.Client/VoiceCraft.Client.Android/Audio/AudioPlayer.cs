using Android.Media;
using System;
using System.Linq;
using System.Threading;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;
using AudioFormat = Android.Media.AudioFormat;

namespace VoiceCraft.Client.Android.Audio
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
                return (Format) switch
                {
                    Core.AudioFormat.Pcm8 => 8,
                    Core.AudioFormat.Pcm16 => 16,
                    Core.AudioFormat.PcmFloat => 32,
                    _ => throw new ArgumentOutOfRangeException(nameof(Format))
                };
            }
        }

        public Core.AudioFormat Format { get; set; }

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

        public AudioUsageKind Usage { get; set; } = AudioUsageKind.Media;

        public AudioContentType ContentType { get; set; } = AudioContentType.Music;

        public int SessionId => _nativePlayer?.AudioSessionId ?? throw new InvalidOperationException("Recorder not intialized!");

        public event Action<Exception?>? OnPlaybackStopped;

        private readonly SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;
        private readonly AudioManager _audioManager;
        private Func<byte[], int, int, int>? _playerCallback;
        private AudioTrack? _nativePlayer;
        private int _sampleRate;
        private int _channels;
        private int _bufferMilliseconds;
        private int _bufferSamples;
        private int _bufferBytes;
        private int _blockAlign;
        private byte[] _byteBuffer = [];
        private float[] _floatBuffer = [];
        private bool _disposed;

        public AudioPlayer(AudioManager audioManager, int sampleRate, int channels, Core.AudioFormat format)
        {
            _audioManager = audioManager;
            SampleRate = sampleRate;
            Channels = channels;
            Format = format;
        }

        ~AudioPlayer()
        {
            //Dispose of this object
            Dispose(false);
        }

        public void Initialize(Func<byte[], int, int, int> playerCallback)
        {
            //Disposed? DIE!
            ThrowIfDisposed();

            //Check if already playing.
            if (PlaybackState != PlaybackState.Stopped)
                throw new InvalidOperationException("Cannot initialize when playing!");

            //Cleanup previous player.
            CleanupPlayer();

            try
            {
                _playerCallback = playerCallback;
                //Create/Open new audio device.
                //Check if the format is supported first.
                var encoding = (BitDepth, Format) switch
                {
                    (8, Core.AudioFormat.Pcm8) => Encoding.Pcm8bit,
                    (16, Core.AudioFormat.Pcm16) => Encoding.Pcm16bit,
                    (32, Core.AudioFormat.PcmFloat) => Encoding.PcmFloat,
                    _ => throw new NotSupportedException()
                };

                //Set the channel type. Only accepts Mono or Stereo
                var channelMask = Channels switch
                {
                    1 => ChannelOut.Mono,
                    2 => ChannelOut.Stereo,
                    _ => throw new NotSupportedException()
                };

                //Determine the buffer size
                _bufferSamples = (BufferMilliseconds + NumberOfBuffers - 1) / NumberOfBuffers * (SampleRate / 1000); //Calculate buffer size IN SAMPLES!
                _bufferBytes = BitDepth / 8 * Channels * _bufferSamples;
                _blockAlign = Channels * (BitDepth / 8);
                if (_bufferBytes % _blockAlign != 0)
                {
                    _bufferBytes -= _bufferBytes % _blockAlign;
                }

                _byteBuffer = new byte[_bufferBytes];
                _floatBuffer = new float[_bufferBytes / sizeof(float)];

                var audioAttributes = new AudioAttributes.Builder().SetUsage(Usage)?.SetContentType(ContentType)?.Build();
                var audioFormat = new AudioFormat.Builder().SetEncoding(encoding)?.SetSampleRate(SampleRate)?.SetChannelMask(channelMask).Build();

                if (audioAttributes == null || audioFormat == null)
                    throw new InvalidOperationException();

                _nativePlayer = new AudioTrack.Builder().SetAudioAttributes(audioAttributes).SetAudioFormat(audioFormat)
                    .SetBufferSizeInBytes(BufferMilliseconds).SetTransferMode(AudioTrackMode.Stream).Build();
                if (_nativePlayer.State != AudioTrackState.Initialized)
                    throw new InvalidOperationException("Could not initialize device!");

                _nativePlayer.SetVolume(1.0f);
                var selectedDevice = _audioManager.GetDevices(GetDevicesTargets.Outputs)
                    ?.FirstOrDefault(x => $"{x.ProductName.Truncate(8)} - {x.Type}" == SelectedDevice);
                _nativePlayer.SetPreferredDevice(selectedDevice);
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

            try
            {
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

            if (PlaybackState != PlaybackState.Playing) return;

            PlaybackState = PlaybackState.Stopping;
            _nativePlayer?.Stop();

            while (PlaybackState == PlaybackState.Stopping)
            {
                Thread.Sleep(1); //Wait until stopped.
            }
        }

        public void Dispose()
        {
            //Dispose of this object
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void CleanupPlayer()
        {
            if (_nativePlayer == null) return;
            _nativePlayer.Stop();
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
            if (_nativePlayer == null)
                throw new InvalidOperationException("Audio player is not intialized!");
        }

        private void Resume()
        {
            if (PlaybackState != PlaybackState.Paused) return;
            _nativePlayer?.Play();
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
                Stop();
                PlaybackState = PlaybackState.Stopped;
                InvokePlaybackStopped(exception);
            }
        }

        private void PlaybackLogic()
        {
            //This shouldn't happen...
            if (_playerCallback == null)
                throw new InvalidOperationException("Player callback was not found!");

            //Run the playback loop
            _nativePlayer?.Play();
            PlaybackState = PlaybackState.Playing;
            while (PlaybackState != PlaybackState.Stopped && _nativePlayer != null)
            {
                //Check the playback state
                if (PlaybackState != PlaybackState.Playing)
                {
                    Thread.Sleep(1);
                    continue;
                }

                if (_nativePlayer.PlayState == PlayState.Stopped)
                    break;

                Array.Clear(_byteBuffer);
                Array.Clear(_floatBuffer);

                //Fill the wave buffer with new samples
                var read = _playerCallback(_byteBuffer, 0, _byteBuffer.Length);
                if (read <= 0) break;
                switch (_nativePlayer.AudioFormat)
                {
                    //Write the specified wave buffer to the audio track
                    case Encoding.Pcm8bit:
                    case Encoding.Pcm16bit:
                    {
                        _nativePlayer.Write(_byteBuffer, 0, read);
                        break;
                    }
                    case Encoding.PcmFloat:
                    {
                        Buffer.BlockCopy(_byteBuffer, 0, _floatBuffer, 0, read);
                        _nativePlayer.Write(_floatBuffer, 0, read / sizeof(float), WriteMode.Blocking);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                _nativePlayer.Flush();
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposing || _disposed) return;
            CleanupPlayer();
            _disposed = true;
        }
    }
}