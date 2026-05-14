using System;
using OpusSharp.Core;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Browser.Audio;

public sealed class BrowserOpusAudioEncoder : IAudioEncoder
{
    private readonly OpusEncoder _opusEncoder = new(
        Constants.SampleRate,
        Constants.RecordingChannels,
        OpusPredefinedValues.OPUS_APPLICATION_VOIP,
        true);

    private bool _disposed;

    public int Encode(Span<float> data, Span<byte> output, int samples)
    {
        ThrowIfDisposed();
        return _opusEncoder.Encode(data, samples, output, output.Length);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _opusEncoder.Dispose();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(typeof(BrowserOpusAudioEncoder).ToString());
    }
}
