using System.Collections.Generic;
using System.Threading.Tasks;
using NAudio.Wave;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Windows.Audio;

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
        var devices = new List<string>();
        for (var n = 0; n < WaveIn.DeviceCount; n++)
        {
            var caps = WaveIn.GetCapabilities(n);
            if (!devices.Contains(caps.ProductName))
                devices.Add(caps.ProductName);
        }

        return Task.FromResult(devices);
    }

    public override Task<List<string>> GetOutputDevicesAsync()
    {
        var devices = new List<string>();
        for (var n = 0; n < WaveOut.DeviceCount; n++)
        {
            var caps = WaveOut.GetCapabilities(n);
            if (!devices.Contains(caps.ProductName))
                devices.Add(caps.ProductName);
        }

        return Task.FromResult(devices);
    }
}