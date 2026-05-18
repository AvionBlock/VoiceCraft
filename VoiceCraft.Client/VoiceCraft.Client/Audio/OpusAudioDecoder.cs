using System;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;
using OpusSharp.Core;

namespace VoiceCraft.Client.Audio;

public class OpusAudioDecoder : IAudioDecoder
{
    private static readonly bool? StaticallyLinkedRuntime = OperatingSystem.IsIOS() ? true : null;
    private readonly OpusDecoder _opusDecoder = new(
        Constants.SampleRate,
        Constants.RecordingChannels,
        StaticallyLinkedRuntime);
    private bool _disposed;

    ~OpusAudioDecoder()
    {
        Dispose(false);
    }
    
    public int Decode(Span<byte> buffer, Span<float> output, int samples)
    {
        ThrowIfDisposed();
        return _opusDecoder.Decode(buffer, buffer.Length, output, samples, false);
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    private void ThrowIfDisposed()
    {
        if (!_disposed) return;
        throw new ObjectDisposedException(typeof(OpusAudioDecoder).ToString());
    }
    
    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            _opusDecoder.Dispose();
        }
        
        _disposed = true;
    }
}
