using System;
using System.Threading;
using OpenTK.Audio.OpenAL;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.Linux.Audio;

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
    private ALCaptureDevice _nativeRecorder = ALCaptureDevice.Null;
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
                throw new InvalidOperationException(Localizer.Get("Audio.Player.InitFailed"));

            //Cleanup previous recorder.
            CleanupRecorder();

            //AL.IsExtensionPresent("AL_EXT_float32") I don't know why this extension checker doesn't work properly.
            //Select Device.
            var format = (Format, Channels) switch
            {
                (AudioFormat.Pcm8, 1) => ALFormat.Mono8,
                (AudioFormat.Pcm8, 2) => ALFormat.Stereo8,
                (AudioFormat.Pcm16, 1) => ALFormat.Mono16,
                (AudioFormat.Pcm16, 2) => ALFormat.Stereo16,
                (AudioFormat.PcmFloat, 1) => ALFormat.MonoFloat32Ext,
                (AudioFormat.PcmFloat, 2) => ALFormat.StereoFloat32Ext,
                _ => throw new NotSupportedException()
            };

            //Setup recorder.
            _bufferSamples = BufferMilliseconds * SampleRate / 1000;
            _bufferBytes = BitDepth / 8 * Channels * _bufferSamples;
            _blockAlign = Channels * (BitDepth / 8);
            if (_bufferBytes % _blockAlign != 0) _bufferBytes -= _bufferBytes % _blockAlign;
            _buffer = new byte[_bufferBytes];

            //Triple sample size so we don't skip any recorder audio.
            _nativeRecorder = ALC.CaptureOpenDevice(SelectedDevice, SampleRate, format, _bufferSamples * 3);
            if (_nativeRecorder == ALCaptureDevice.Null)
                throw new InvalidOperationException(Localizer.Get("Audio.Player.InitFailed"));
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
            ALC.CaptureStop(_nativeRecorder);
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
        if (_nativeRecorder == ALCaptureDevice.Null) return;
        ALC.CaptureStop(_nativeRecorder);
        ALC.CaptureCloseDevice(_nativeRecorder);
        _nativeRecorder = ALCaptureDevice.Null;
    }

    private void ThrowIfDisposed()
    {
        if (!_disposed) return;
        throw new ObjectDisposedException(typeof(AudioPlayer).ToString());
    }

    private void ThrowIfNotInitialized()
    {
        if (_nativeRecorder == ALCaptureDevice.Null)
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
            ALC.CaptureStop(_nativeRecorder);
            InvokeRecordingStopped(exception);
        }
    }

    private unsafe void RecordingLogic()
    {
        ALC.CaptureStart(_nativeRecorder);
        CaptureState = CaptureState.Capturing;
        var capturedSamples = 0;

        //Run the record loop
        while (CaptureState == CaptureState.Capturing && _nativeRecorder != ALCaptureDevice.Null)
        {
            // Query the number of captured samples
            ALC.GetInteger(_nativeRecorder, AlcGetInteger.CaptureSamples, sizeof(int), &capturedSamples);

            if (capturedSamples < _bufferSamples)
            {
                Thread.Sleep(1); //So we don't exactly burn the CPU. Small hack but it works.
                continue;
            }

            Array.Clear(_buffer);
            fixed (void* bufferPtr = _buffer)
            {
                ALC.CaptureSamples(_nativeRecorder, bufferPtr, _bufferSamples);
            }

            InvokeDataAvailable(_buffer, _bufferBytes);
        }
    }

    private void Dispose(bool _)
    {
        if (_disposed) return;

        //Recorder isn't managed.
        CleanupRecorder();

        _disposed = true;
    }
}