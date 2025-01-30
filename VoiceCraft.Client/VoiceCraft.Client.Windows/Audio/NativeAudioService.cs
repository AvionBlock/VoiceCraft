using System.Collections.Generic;
using NAudio.Wave;
using OpenTK.Audio.OpenAL;
using VoiceCraft.Client.Audio.Interfaces;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Windows.Audio;

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
        var devices = new List<string>();
        for (var n = 0; n < WaveIn.DeviceCount; n++)
        {
            var caps = WaveIn.GetCapabilities(n);
            if (!devices.Contains(caps.ProductName))
                devices.Add(caps.ProductName);
        }

        return devices;
    }

    public override List<string> GetOutputDevices()
    {
        var devices = new List<string>();
        for (var n = 0; n < WaveOut.DeviceCount; n++)
        {
            var caps = WaveOut.GetCapabilities(n);
            if (!devices.Contains(caps.ProductName))
                devices.Add(caps.ProductName);
        }

        return devices;
    }
}