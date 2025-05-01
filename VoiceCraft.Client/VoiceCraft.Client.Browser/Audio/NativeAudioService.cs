using System.Collections.Generic;
using VoiceCraft.Client.Browser.Audio;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Browser.Audio;

public class NativeAudioService : AudioService
{
    public override IAudioRecorder CreateAudioRecorder(int sampleRate, int channels, AudioFormat format)
    {
        return new AudioRecorder(sampleRate, channels, format);
    }

    public override IAudioPlayer CreateAudioPlayer(int sampleRate, int channels, AudioFormat format)
    {
        return new AudioPlayer(sampleRate, channels, format);
    }

    public override List<string> GetInputDevices()
    {
        var list = new List<string>();
        
        return list;
    }

    public override List<string> GetOutputDevices()
    {
        var list = new List<string>();
        
        return list;
    }
}