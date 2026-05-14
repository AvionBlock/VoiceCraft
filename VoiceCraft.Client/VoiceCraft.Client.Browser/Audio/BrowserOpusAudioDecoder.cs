using System;
using OpusSharp.Core;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Browser.Audio;

public sealed class BrowserOpusAudioDecoder : IAudioDecoder
{
    private readonly OpusDecoder _opusDecoder = new(Constants.SampleRate, Constants.RecordingChannels, true);
    private bool _disposed;

    public int Decode(Span<byte> buffer, Span<float> output, int samples)
    {
        ThrowIfDisposed();
        return _opusDecoder.Decode(buffer, buffer.Length, output, samples, false);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _opusDecoder.Dispose();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(typeof(BrowserOpusAudioDecoder).ToString());
    }
}
