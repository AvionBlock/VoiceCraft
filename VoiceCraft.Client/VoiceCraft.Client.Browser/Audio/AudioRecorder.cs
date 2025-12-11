using System;
using System.Threading;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Browser.Audio;

public class AudioRecorder : IAudioRecorder
{
    //Privates
    private readonly Lock _lockObj = new();
    private readonly SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;
    private int _blockAlign;
    private byte[] _buffer = [];
    private int _bufferBytes;
    private int _bufferMilliseconds;
    private int _bufferSamples;
    private int _channels;
    private bool _disposed;
    private int _sampleRate;

    public AudioRecorder(int sampleRate, int channels, AudioFormat format)
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
                throw new InvalidOperationException(Locales.Locales.Audio_Recorder_InitFailed);

            //Cleanup previous recorder.
            CleanupRecorder();

            //Setup recorder.
            _bufferSamples = BufferMilliseconds * SampleRate / 1000;
            _bufferBytes = BitDepth / 8 * Channels * _bufferSamples;
            _blockAlign = Channels * (BitDepth / 8);
            if (_bufferBytes % _blockAlign != 0) _bufferBytes -= _bufferBytes % _blockAlign;
            _buffer = new byte[_bufferBytes];
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

    ~AudioRecorder()
    {
        Dispose(false);
    }

    private void CleanupRecorder()
    {
    }

    private void ThrowIfDisposed()
    {
        if (!_disposed) return;
        throw new ObjectDisposedException(typeof(AudioPlayer).ToString());
    }

    private void ThrowIfNotInitialized()
    {
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
            InvokeRecordingStopped(exception);
        }
    }

    private void RecordingLogic()
    {
    }

    private void Dispose(bool _)
    {
        if (_disposed) return;

        //Recorder isn't managed.
        CleanupRecorder();

        _disposed = true;
    }
}