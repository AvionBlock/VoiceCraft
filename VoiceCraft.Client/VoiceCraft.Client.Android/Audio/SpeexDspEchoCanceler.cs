using System;
using SpeexDSPSharp.Core;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.Android.Audio;

public class SpeexDspEchoCanceler : IEchoCanceler
{
    private SampleBufferProvider<byte>? _captureBuffer;
    private byte[] _captureBufferFrame = [];
    private bool _disposed;
    private SpeexDSPEchoCanceler? _echoCanceler;
    private byte[] _outputBuffer = [];
    public int FilterLengthMs { get; set; } = 100;

    public bool IsNative => false;

    public void Initialize(IAudioRecorder recorder, IAudioPlayer player)
    {
        ThrowIfDisposed();

        if (recorder.SampleRate != player.SampleRate)
            throw new InvalidOperationException(Localizer.Get("Audio.AEC.InitFailed"));

        CleanupEchoCanceler();

        var bufferSamples =
            recorder.BufferMilliseconds * recorder.SampleRate / 1000; //Calculate buffer size IN SAMPLES!
        var bufferBytes = recorder.BitDepth / 8 * recorder.Channels * bufferSamples;
        var filterLengthSamples = FilterLengthMs * recorder.SampleRate / 1000;
        var filterLengthBytes = player.BitDepth / 8 * player.Channels * filterLengthSamples;
        _echoCanceler = new SpeexDSPEchoCanceler(
            bufferSamples,
            filterLengthSamples,
            recorder.Channels,
            player.Channels);
        _captureBuffer = new SampleBufferProvider<byte>(filterLengthBytes);
        _captureBufferFrame = new byte[filterLengthBytes];
        _outputBuffer = new byte[bufferBytes];

        var sampleRate = recorder.SampleRate;
        _echoCanceler.Ctl(EchoCancellationCtl.SPEEX_ECHO_SET_SAMPLING_RATE, ref sampleRate);
    }

    public void EchoCancel(Span<byte> buffer)
    {
        ThrowIfDisposed();
        ThrowIfNotInitialized();
        Array.Clear(_outputBuffer, 0, _outputBuffer.Length);

        _echoCanceler?.EchoCancel(buffer, GetCaptureBufferFrame(), _outputBuffer);
        _outputBuffer.CopyTo(buffer);
    }

    public void EchoPlayback(Span<byte> buffer)
    {
        ThrowIfDisposed();
        ThrowIfNotInitialized();
        _captureBuffer?.Write(buffer);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~SpeexDspEchoCanceler()
    {
        Dispose(false);
    }

    private byte[] GetCaptureBufferFrame()
    {
        if (_captureBuffer == null)
            return _captureBufferFrame;

        lock (_captureBuffer)
        {
            Array.Clear(_captureBufferFrame);
            if (_captureBuffer.Count < _captureBufferFrame.Length)
                return _captureBufferFrame;

            _captureBuffer.Read(_captureBufferFrame);
            return _captureBufferFrame;
        }
    }

    private void CleanupEchoCanceler()
    {
        if (_echoCanceler == null) return;
        _echoCanceler.Dispose();
        _echoCanceler = null;

        if (_captureBuffer == null) return;
        lock (_captureBuffer)
        {
            _captureBuffer = null;
        }
    }

    private void ThrowIfDisposed()
    {
        if (!_disposed) return;
        throw new ObjectDisposedException(typeof(SpeexDspEchoCanceler).ToString());
    }

    private void ThrowIfNotInitialized()
    {
        if (_echoCanceler == null || _captureBuffer == null)
            throw new InvalidOperationException(Localizer.Get("Audio.AEC.Init"));
    }

    private void Dispose(bool disposing)
    {
        if (_disposed || !disposing) return;
        CleanupEchoCanceler();
        _disposed = true;
    }
}