using System.Collections.Generic;
using System.Diagnostics;
using OpenTK.Audio.OpenAL;
using VoiceCraft.Client.Audio.Interfaces;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Linux.Audio;

public class NativeAudioService : AudioService
{
    public override IAudioRecorder CreateAudioRecorder()
    {
        return new AudioRecorder();
    }

    public override IAudioPlayer CreateAudioPlayer()
    {
        return new AudioPlayer();
    }

    public override List<string> GetInputDevices()
    {
        var list = new List<string>();

        var devices = ALC.GetString(ALDevice.Null, AlcGetStringList.CaptureDeviceSpecifier);
        list.AddRange(devices);
        
        return list;
    }

    public override List<string> GetOutputDevices()
    {
        var list = new List<string>();
        
        var devices = ALC.GetString(ALDevice.Null, AlcGetStringList.AllDevicesSpecifier);
        list.AddRange(devices);
        
        return list;
    }
}