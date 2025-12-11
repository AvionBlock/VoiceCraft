using System;
using System.Threading;
using NAudio.Wave;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;
using PlaybackState = VoiceCraft.Core.PlaybackState;

namespace VoiceCraft.Client.Windows.Audio;

public class AudioPlayer : IAudioPlayer
{
    //Privates
    private readonly Lock _lockObj = new();
    private int _bufferMilliseconds;
    private int _channels;
    private bool _disposed;
    private WaveOutEvent? _nativePlayer;
    private int _sampleRate;

    public AudioPlayer(int sampleRate, int channels, AudioFormat format)
    {
        SampleRate = sampleRate;
        Channels = channels;
        Format = format;
    }

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

            if (PlaybackState != PlaybackState.Stopped)
                throw new InvalidOperationException(Locales.Locales.Audio_Player_InitFailed);

            //Cleanup previous player.
            CleanupPlayer();

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
                _ => throw new NotSupportedException()
            };

            var callbackProvider = new CallbackWaveProvider(waveFormat, playerCallback);

            //Setup Player
            _nativePlayer = new WaveOutEvent();
            _nativePlayer.DesiredLatency = BufferMilliseconds;
            _nativePlayer.DeviceNumber = selectedDevice;
            _nativePlayer.Volume = 1.0f;
            _nativePlayer.NumberOfBuffers = 3;
            _nativePlayer.PlaybackStopped += InvokePlaybackStopped;
            _nativePlayer.Init(callbackProvider);
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

            if (PlaybackState != PlaybackState.Stopped) return;

            PlaybackState = PlaybackState.Starting;
            _nativePlayer?.Play();
            PlaybackState = PlaybackState.Playing;
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
            _nativePlayer?.Pause();
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
            if (PlaybackState is not (PlaybackState.Playing or PlaybackState.Paused)) return;

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
        //Dispose of this object.
        Dispose(false);
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
        if (_nativePlayer == null)
            throw new InvalidOperationException(Locales.Locales.Audio_Player_Init);
    }

    private void InvokePlaybackStopped(object? sender, StoppedEventArgs e)
    {
        PlaybackState = PlaybackState.Stopped;
        OnPlaybackStopped?.Invoke(e.Exception);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing || _disposed) return;
        CleanupPlayer();
        _disposed = true;
    }

    private class CallbackWaveProvider(WaveFormat waveFormat, Func<byte[], int, int> callback) : IWaveProvider
    {
        public WaveFormat WaveFormat { get; } = waveFormat;

        public int Read(byte[] buffer, int _, int count)
        {
            return callback(buffer, count);
        }
    }
}