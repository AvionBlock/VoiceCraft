using System;
using System.Buffers;
using SpeexDSPSharp.Core;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.MacOS.Audio;

public class SpeexDspPreprocessor : IAudioPreprocessor
{
    private readonly SpeexDSPPreprocessor _preprocessor;
    private readonly SpeexDSPEchoCanceler _echoCanceler;
    private readonly SampleBufferProvider<short> _captureBuffer;
    private readonly short[] _captureBufferFrame;
    private bool _echoCancelEnabled;
    private bool _disposed;

    public bool DenoiserEnabled
    {
        get
        {
            ThrowIfDisposed();
            var result = 0;
            _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_GET_DENOISE, ref result);
            return result == 1;
        }
        set
        {
            ThrowIfDisposed();
            _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_DENOISE, ref value);
        }
    }

    public bool GainControllerEnabled
    {
        get
        {
            ThrowIfDisposed();
            var result = 0;
            _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_GET_AGC, ref result);
            return result == 1;
        }
        set
        {
            ThrowIfDisposed();
            _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_AGC, ref value);
        }
    }

    public bool EchoCancelerEnabled
    {
        get
        {
            ThrowIfDisposed();
            return _echoCancelEnabled;
        }
        set
        {
            ThrowIfDisposed();
            _echoCancelEnabled = value;
        }
    }

    public int TargetGain
    {
        get
        {
            ThrowIfDisposed();
            var result = 0;
            _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_GET_AGC_TARGET, ref result);
            return result;
        }
        set
        {
            ThrowIfDisposed();
            _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_AGC_TARGET, ref value);
        }
    }

    public SpeexDspPreprocessor(int sampleRate, int frameSize, int nbMicrophones, int nbSpeakers)
    {
        if(nbMicrophones != 1)
            throw new ArgumentException("Number of microphones must be 1!");
        var filterLength = 100 * sampleRate / 1000;

        _preprocessor = new SpeexDSPPreprocessor(frameSize, sampleRate);
        _echoCanceler = new SpeexDSPEchoCanceler(frameSize, filterLength, 1, nbSpeakers);
        _captureBuffer = new SampleBufferProvider<short>(filterLength * nbSpeakers);
        _captureBufferFrame = new short[frameSize * nbSpeakers];
        DenoiserEnabled = true;
        GainControllerEnabled = true;
        EchoCancelerEnabled = true;
        TargetGain = 26000;

        _echoCanceler.Ctl(EchoCancellationCtl.SPEEX_ECHO_SET_SAMPLING_RATE, ref sampleRate);
    }

    ~SpeexDspPreprocessor()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Process(Span<float> buffer)
    {
        ThrowIfDisposed();
        var shortBuffer = ArrayPool<short>.Shared.Rent(buffer.Length);
        var shortSpanBuffer = shortBuffer.AsSpan();
        shortSpanBuffer.Clear();

        try
        {
            var read = SampleFloatTo16.Read(buffer, shortSpanBuffer);
            _preprocessor.Run(shortSpanBuffer);
            if(_echoCancelEnabled)
                ProcessEchoCancel(shortSpanBuffer);
            Sample16ToFloat.Read(shortSpanBuffer[..read], buffer);
        }
        finally
        {
            ArrayPool<short>.Shared.Return(shortBuffer);
        }
    }

    public void ProcessPlayback(Span<float> buffer)
    {
        ThrowIfDisposed();
        var shortBuffer = ArrayPool<short>.Shared.Rent(buffer.Length);
        var shortSpanBuffer = shortBuffer.AsSpan();
        shortSpanBuffer.Clear();
        try
        {
            lock (_captureBuffer)
            {
                var read = SampleFloatTo16.Read(buffer, shortSpanBuffer);
                _captureBuffer.Write(shortSpanBuffer[..read]);
            }
        }
        finally
        {
            ArrayPool<short>.Shared.Return(shortBuffer);
        }
    }

    private void ProcessEchoCancel(Span<short> buffer)
    {
        lock (_captureBuffer)
        {
            _captureBufferFrame.AsSpan().Clear();
            if (_captureBuffer.Count >= _captureBufferFrame.Length)
            {
                _captureBuffer.Read(_captureBufferFrame);
            }

            _echoCanceler.EchoCancel(buffer, _captureBufferFrame, buffer);
        }
    }

    private void ThrowIfDisposed()
    {
        if (!_disposed) return;
        throw new ObjectDisposedException(typeof(SpeexDspPreprocessor).ToString());
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            _preprocessor.Dispose();
            _echoCanceler.Dispose();
        }

        _disposed = true;
    }
}
