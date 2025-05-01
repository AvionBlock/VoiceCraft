using System.Collections.Generic;
using System.Threading.Tasks;
using OpenTK.Audio.OpenAL;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Linux.Audio;

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

    public override Task<List<string>> GetInputDevicesAsync()
    {
        var list = new List<string>();

        var devices = ALC.GetString(ALDevice.Null, AlcGetStringList.CaptureDeviceSpecifier);
        list.AddRange(devices);
        
        return Task.FromResult(list);
    }

    public override Task<List<string>> GetOutputDevicesAsync()
    {
        var list = new List<string>();
        
        var devices = ALC.GetString(ALDevice.Null, AlcGetStringList.AllDevicesSpecifier);
        list.AddRange(devices);
        
        return Task.FromResult(list);
    }
}