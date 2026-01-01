using System;
using System.Linq;
using System.Threading;
using Android.Media;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Locales;
using AudioFormat = VoiceCraft.Core.AudioFormat;

namespace VoiceCraft.Client.Android.Audio;

public class AudioRecorder : IAudioRecorder
{
    private readonly AudioManager _audioManager;

    //Privates
    private readonly Lock _lockObj = new();
    private readonly SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;
    private int _bufferBytes;
    private int _bufferMilliseconds;
    private byte[] _byteBuffer = [];
    private int _channels;
    private bool _disposed;
    private float[] _floatBuffer = [];
    private AudioRecord? _nativeRecorder;
    private int _sampleRate;

    public AudioRecorder(AudioManager audioManager, int sampleRate, int channels, AudioFormat format)
    {
        _audioManager = audioManager;
        SampleRate = sampleRate;
        Channels = channels;
        Format = format;
    }

    public AudioSource AudioSource { get; set; } = AudioSource.VoiceCommunication;

    public int SessionId => _nativeRecorder?.AudioSessionId ??
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

    public CaptureState CaptureState { get; private set; }

    public event Action<byte[], int>? OnDataAvailable;
    public event Action<Exception?>? OnRecordingStopped;

    public void Initialize()
    {
        _lockObj.Enter();

        try
        {
            //Disposed? DIE!
            ThrowIfDisposed();

            if (CaptureState != CaptureState.Stopped)
                throw new InvalidOperationException(Localizer.Get("Audio.Player.InitFailed"));

            //Cleanup previous recorder.
            CleanupRecorder();

            //Set the encoding
            var encoding = Format switch
            {
                AudioFormat.Pcm8 => Encoding.Pcm8bit,
                AudioFormat.Pcm16 => Encoding.Pcm16bit,
                AudioFormat.PcmFloat => Encoding.PcmFloat,
                _ => throw new NotSupportedException()
            };

            //Set the channel type. Only accepts Mono or Stereo
            var channelMask = Channels switch
            {
                1 => ChannelIn.Mono,
                2 => ChannelIn.Stereo,
                _ => throw new NotSupportedException()
            };

            //Determine the buffer size
            var blockAlign = Channels * (BitDepth / 8);
            var bytesPerSecond = _sampleRate * blockAlign;
            _bufferBytes = BufferMilliseconds * bytesPerSecond / 1000;
            if (_bufferBytes % blockAlign != 0)
                _bufferBytes -= _bufferBytes % blockAlign;

            _byteBuffer = new byte[_bufferBytes];
            _floatBuffer = new float[_bufferBytes / sizeof(float)];

            //Create the AudioRecord Object.
            _nativeRecorder = new AudioRecord(AudioSource, SampleRate, channelMask, encoding, _bufferBytes);
            if (_nativeRecorder.State != State.Initialized)
                throw new InvalidOperationException(Localizer.Get("Audio.Player.InitFailed"));

            var device = _audioManager.GetDevices(GetDevicesTargets.Inputs)
                ?.FirstOrDefault(x => $"{x.ProductName?.Truncate(8)} - {x.Type}" == SelectedDevice);
            _nativeRecorder.SetPreferredDevice(device);
        }
        catch
        {
            CleanupRecorder();
            throw;
        }
        finally
        {
            _lockObj.Exit();
        }
    }

    public void Start()
    {
        _lockObj.Enter();

        try
        {
            //Disposed? DIE!
            ThrowIfDisposed();
            ThrowIfNotInitialized();
            if (CaptureState != CaptureState.Stopped) return;

            CaptureState = CaptureState.Starting;
            ThreadPool.QueueUserWorkItem(_ => RecordThread(), null);
        }
        catch
        {
            CaptureState = CaptureState.Stopped;
            throw;
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
            if (CaptureState != CaptureState.Capturing) return;

            CaptureState = CaptureState.Stopping;
            _nativeRecorder?.Stop();
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

    ~AudioRecorder()
    {
        Dispose(false);
    }

    private void CleanupRecorder()
    {
        if (_nativeRecorder == null) return;
        _nativeRecorder.Stop();
        _nativeRecorder.Dispose();
        _nativeRecorder = null;
    }

    private void ThrowIfDisposed()
    {
        if (!_disposed) return;
        throw new ObjectDisposedException(typeof(AudioPlayer).ToString());
    }

    private void ThrowIfNotInitialized()
    {
        if (_nativeRecorder == null)
            throw new InvalidOperationException(Localizer.Get("Audio.Player.Init"));
    }

    private void InvokeDataAvailable(byte[] buffer, int bytesRecorded)
    {
        CaptureState = CaptureState.Capturing;
        OnDataAvailable?.Invoke(buffer, bytesRecorded);
    }

    private void InvokeRecordingStopped(Exception? exception = null)
    {
        CaptureState = CaptureState.Stopped;
        var handler = OnRecordingStopped;
        if (handler == null) return;
        if (_synchronizationContext == null)
            handler(exception);
        else
            _synchronizationContext.Post(_ => handler(exception), null);
    }

    private void RecordThread()
    {
        Exception? exception = null;
        try
        {
            RecordingLogic();
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        finally
        {
            if (_nativeRecorder != null && _nativeRecorder.RecordingState != RecordState.Stopped)
                _nativeRecorder?.Stop();
            InvokeRecordingStopped(exception);
        }
    }

    private void RecordingLogic()
    {
        _nativeRecorder?.StartRecording();
        CaptureState = CaptureState.Capturing;

        //Run the record loop
        while (CaptureState == CaptureState.Capturing && _nativeRecorder != null)
        {
            Array.Clear(_byteBuffer);
            Array.Clear(_floatBuffer);

            switch (_nativeRecorder.AudioFormat)
            {
                case Encoding.Pcm8bit:
                case Encoding.Pcm16bit:
                {
                    var bytesRead = _nativeRecorder.Read(_byteBuffer, 0, _byteBuffer.Length);
                    if (bytesRead > 0)
                        InvokeDataAvailable(_byteBuffer, bytesRead);
                    break;
                }
                case Encoding.PcmFloat:
                {
                    var floatsRead = _nativeRecorder.Read(_floatBuffer, 0, _floatBuffer.Length, 0);
                    if (floatsRead > 0)
                    {
                        Buffer.BlockCopy(_floatBuffer, 0, _byteBuffer, 0, _byteBuffer.Length);
                        InvokeDataAvailable(_byteBuffer, floatsRead * sizeof(float));
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void Dispose(bool disposing)
    {
        if (_disposed || !disposing) return;
        CleanupRecorder();
        _disposed = true;
    }
}