using System;
using System.Linq;
using System.Threading;
using Android.Media;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Locales;
using AudioFormat = Android.Media.AudioFormat;

namespace VoiceCraft.Client.Android.Audio;

public class AudioPlayer : IAudioPlayer
{
    private const int NumberOfBuffers = 3;
    private readonly AudioManager _audioManager;

    private readonly Lock _lockObj = new();
    private readonly SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;
    private int _bufferBytes;
    private int _bufferMilliseconds;
    private byte[] _byteBuffer = [];
    private int _channels;
    private bool _disposed;
    private float[] _floatBuffer = [];
    private AudioTrack? _nativePlayer;
    private Func<byte[], int, int>? _playerCallback;
    private int _sampleRate;
    private short[] _shortBuffer = [];

    public AudioPlayer(AudioManager audioManager, int sampleRate, int channels, Core.AudioFormat format)
    {
        _audioManager = audioManager;
        SampleRate = sampleRate;
        Channels = channels;
        Format = format;
    }

    public AudioUsageKind Usage { get; set; } = AudioUsageKind.Media;

    public AudioContentType ContentType { get; set; } = AudioContentType.Music;

    public int SessionId => _nativePlayer?.AudioSessionId ??
                            throw new InvalidOperationException(Localizer.Get("Audio.Player.Init"));

    //Public Properties
    public int SampleRate
    {
        get => _sampleRate;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    "Sample rate must be greater than or equal to zero!");

            _sampleRate = value;
        }
    }

    public int Channels
    {
        get => _channels;
        set
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    "Channels must be greater than or equal to one!");

            _channels = value;
        }
    }

    public int BitDepth
    {
        get
        {
            return Format switch
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
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    "Buffer milliseconds must be greater than or equal to zero!");

            _bufferMilliseconds = value;
        }
    }

    public string? SelectedDevice { get; set; }

    public PlaybackState PlaybackState { get; private set; }

    public event Action<Exception?>? OnPlaybackStopped;

    public void Initialize(Func<byte[], int, int> playerCallback)
    {
        _lockObj.Enter();

        try
        {
            //Disposed? DIE!
            ThrowIfDisposed();

            //Check if already playing.
            if (PlaybackState != PlaybackState.Stopped)
                throw new InvalidOperationException(Localizer.Get("Audio.Player.InitFailed"));

            //Cleanup previous player.
            CleanupPlayer();

            _playerCallback = playerCallback;
            //Check if the format is supported first.
            var encoding = Format switch
            {
                Core.AudioFormat.Pcm8 => Encoding.Pcm8bit,
                Core.AudioFormat.Pcm16 => Encoding.Pcm16bit,
                Core.AudioFormat.PcmFloat => Encoding.PcmFloat,
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
            var blockAlign = Channels * (BitDepth / 8);
            var bytesPerSecond = _sampleRate * blockAlign;

            var audioAttributes = new AudioAttributes.Builder().SetUsage(Usage)?.SetContentType(ContentType)?.Build();
            var audioFormat = new AudioFormat.Builder().SetEncoding(encoding)?.SetSampleRate(SampleRate)
                ?.SetChannelMask(channelMask).Build();

            if (audioAttributes == null || audioFormat == null)
                throw new InvalidOperationException();

            //Calculate total buffer bytes.
            var totalBufferBytes = (int)(bytesPerSecond / 1000.0 * BufferMilliseconds);
            if (totalBufferBytes % blockAlign != 0)
                totalBufferBytes = totalBufferBytes + blockAlign - totalBufferBytes % blockAlign;
            totalBufferBytes = Math.Max(totalBufferBytes,
                AudioTrack.GetMinBufferSize(SampleRate, channelMask, encoding));

            _nativePlayer = new AudioTrack.Builder().SetAudioAttributes(audioAttributes).SetAudioFormat(audioFormat)
                .SetBufferSizeInBytes(totalBufferBytes).SetTransferMode(AudioTrackMode.Stream).Build();

            _bufferBytes = (_nativePlayer.BufferSizeInFrames + NumberOfBuffers - 1) / NumberOfBuffers * blockAlign;
            _bufferBytes = (_bufferBytes + 3) & ~3;
            _byteBuffer = new byte[_bufferBytes];
            _shortBuffer = new short[_bufferBytes / sizeof(short)];
            _floatBuffer = new float[_bufferBytes / sizeof(float)];

            if (_nativePlayer.State != AudioTrackState.Initialized)
                throw new InvalidOperationException(Localizer.Get("Audio.Player.InitFailed"));

            _nativePlayer.SetVolume(1.0f);
            var selectedDevice = _audioManager.GetDevices(GetDevicesTargets.Outputs)
                ?.FirstOrDefault(x =>
                    x.ProductName != null && $"{x.ProductName.Truncate(8)} - {x.Type}" == SelectedDevice);
            _nativePlayer.SetPreferredDevice(selectedDevice);
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

            _nativePlayer?.Pause();
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
            _nativePlayer?.Stop();
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

    ~AudioPlayer()
    {
        //Dispose of this object
        Dispose(false);
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
            throw new InvalidOperationException(Localizer.Get("Audio.Player.Init"));
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
            handler(exception);
        else
            _synchronizationContext.Post(_ => handler(exception), null);
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
            if (_nativePlayer != null && _nativePlayer.PlayState != PlayState.Stopped)
                _nativePlayer.Stop();
            InvokePlaybackStopped(exception);
        }
    }

    private void PlaybackLogic()
    {
        //This shouldn't happen...
        if (_playerCallback == null || _nativePlayer == null)
            throw new InvalidOperationException();

        //Run the playback loop
        _nativePlayer.Play();
        PlaybackState = PlaybackState.Playing;
        while (PlaybackState != PlaybackState.Stopped && _nativePlayer != null)
        {
            if (_nativePlayer.PlayState == PlayState.Stopped)
                break;
            if (_nativePlayer.PlayState == PlayState.Paused) //Paused, Sleep then recheck.
            {
                Thread.Sleep(1);
                continue;
            }

            Array.Clear(_byteBuffer);

            //Fill the wave buffer with new samples
            var read = _playerCallback(_byteBuffer, _bufferBytes);
            if (read <= 0) break;
            switch (_nativePlayer.AudioFormat)
            {
                //Write the specified wave buffer to the audio track
                case Encoding.Pcm8bit:
                {
                    _nativePlayer.Write(_byteBuffer, 0, read, WriteMode.Blocking);
                    break;
                }
                case Encoding.Pcm16bit:
                {
                    Array.Clear(_shortBuffer);
                    Buffer.BlockCopy(_byteBuffer, 0, _shortBuffer, 0, read);
                    _nativePlayer.Write(_shortBuffer, 0, read / sizeof(short), WriteMode.Blocking); //Shit's annoying.
                    break;
                }
                case Encoding.PcmFloat:
                {
                    Array.Clear(_floatBuffer);
                    Buffer.BlockCopy(_byteBuffer, 0, _floatBuffer, 0, read);
                    _nativePlayer.Write(_floatBuffer, 0, read / sizeof(float), WriteMode.Blocking);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        _nativePlayer?.Flush();
    }

    private void Dispose(bool disposing)
    {
        if (!disposing || _disposed) return;
        CleanupPlayer();
        _disposed = true;
    }
}