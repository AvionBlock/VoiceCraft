using System;
using OpusSharp.Core;
using OpusSharp.Core.Extensions;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Windows.Audio;

public class NativeAudioEncoder: IAudioEncoder
{
    private readonly OpusEncoder _opusEncoder;

    public NativeAudioEncoder()
    {
        _opusEncoder = new OpusEncoder(Constants.SampleRate, Constants.Channels,
            OpusPredefinedValues.OPUS_APPLICATION_VOIP);
        _opusEncoder.SetPacketLostPercent(20); //Expected packet loss, might make this change over time later.
        _opusEncoder.SetBitRate(32000);
    }

    public int Encode(Span<float> data, Span<byte> output, int samples)
    {
        return _opusEncoder.Encode(data, samples, output, output.Length);
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
            _opusEncoder.Dispose();
        }
    }
}