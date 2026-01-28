using System;
using OpusSharp.Core;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Windows.Audio;

public class NativeAudioDecoder : IAudioDecoder
{
    private readonly OpusDecoder _opusDecoder = new(Constants.SampleRate, Constants.Channels);
    
    public int Decode(Span<byte> buffer, Span<float> output, int samples)
    {
        return _opusDecoder.Decode(buffer, buffer.Length, output, samples, false);
    }
    
    public void Dispose()
    {
        Dispose(false);
        GC.SuppressFinalize(this);
    }
    
    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _opusDecoder.Dispose();
        }
    }
}