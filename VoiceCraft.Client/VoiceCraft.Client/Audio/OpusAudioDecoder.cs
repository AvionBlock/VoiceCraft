using System;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;
using OpusSharp.Core;

namespace VoiceCraft.Client.Audio;

public class OpusAudioDecoder : IAudioDecoder
{
    private readonly OpusDecoder _opusDecoder = new(Constants.SampleRate, Constants.RecordingChannels);
    
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