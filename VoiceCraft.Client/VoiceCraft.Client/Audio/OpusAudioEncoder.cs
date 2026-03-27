using System;
using OpusSharp.Core;
using OpusSharp.Core.Extensions;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Audio;

public class OpusAudioEncoder: IAudioEncoder
{
    private readonly OpusEncoder _opusEncoder;
    private bool _disposed;

    public OpusAudioEncoder()
    {
        _opusEncoder = new OpusEncoder(Constants.SampleRate, Constants.RecordingChannels,
            OpusPredefinedValues.OPUS_APPLICATION_VOIP);
        _opusEncoder.SetPacketLostPercent(20); //Expected packet loss, might make this change over time later.
        _opusEncoder.SetBitRate(32000);
    }

    ~OpusAudioEncoder()
    {
        Dispose(false);
    }

    public int Encode(Span<float> data, Span<byte> output, int samples)
    {
        ThrowIfDisposed();
        return _opusEncoder.Encode(data, samples, output, output.Length);
    }
    
    public void Dispose()
    {
        Dispose(false);
        GC.SuppressFinalize(this);
    }
    
    private void ThrowIfDisposed()
    {
        if (!_disposed) return;
        throw new ObjectDisposedException(typeof(OpusAudioEncoder).ToString());
    }
    
    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            _opusEncoder.Dispose();
        }
        
        _disposed = true;
    }
}