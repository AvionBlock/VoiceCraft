using System.Collections.Generic;
using VoiceCraft.Client.Audio.Interfaces;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Linux.Audio;

public class NativeAudioService : AudioService
{
    public override IAudioRecorder CreateAudioRecorder()
    {
        throw new System.NotImplementedException();
    }

    public override IAudioPlayer CreateAudioPlayer()
    {
        throw new System.NotImplementedException();
    }
    
    public override string GetDefaultInputDevice()
    {
        return "Default";
    }

    public override string GetDefaultOutputDevice()
    {
        return "Default";
    }

    public override List<string> GetInputDevices()
    {
        var list = new List<string>() { GetDefaultInputDevice() };
        return list;
    }

    public override List<string> GetOutputDevices()
    {
        var list = new List<string>() { GetDefaultInputDevice() };
        return list;
    }
}