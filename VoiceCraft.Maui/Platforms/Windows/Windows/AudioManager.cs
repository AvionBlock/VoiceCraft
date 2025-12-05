using NAudio.Wave;
using VoiceCraft.Maui.Interfaces;
using VoiceCraft.Maui.Services;

namespace VoiceCraft.Maui;

public class AudioManager : IAudioManager
{
    public static AudioManager Instance { get; } = new AudioManager();

    public IWavePlayer CreatePlayer(ISampleProvider waveProvider)
    {
        var settings = Database.Instance.Settings;
        var deviceNumber = settings.OutputDevice - 1;

        if (deviceNumber >= WaveOut.DeviceCount)
        {
            deviceNumber = -1; // Fallback to default
        }

        var Player = new WaveOutEvent();
        Player.DesiredLatency = 50;
        Player.NumberOfBuffers = 3;
        Player.DeviceNumber = deviceNumber;
        Player.Init(waveProvider);
        return Player;
    }

    public IWaveIn CreateRecorder(WaveFormat waveFormat, int bufferMS)
    {
        var settings = Database.Instance.Settings;
        var deviceNumber = settings.InputDevice - 1;

        if (deviceNumber >= WaveIn.DeviceCount)
        {
            deviceNumber = -1; // Fallback to default
        }

        var Recorder = new WaveInEvent();
        Recorder.WaveFormat = waveFormat;
        Recorder.BufferMilliseconds = bufferMS;
        Recorder.DeviceNumber = deviceNumber;
        return Recorder;
    }

    public string[] GetInputDevices()
    {
        var inputDevices = new List<string>();

        for (int i = 0; i < WaveIn.DeviceCount; i++)
            inputDevices.Add(WaveIn.GetCapabilities(i).ProductName);

        return inputDevices.ToArray();
    }

    public string[] GetOutputDevices()
    {
        var outputDevices = new List<string>();

        for (int i = 0; i < WaveOut.DeviceCount; i++)
            outputDevices.Add(WaveOut.GetCapabilities(i).ProductName);

        return outputDevices.ToArray();
    }

    public int GetInputDeviceCount()
    {
        return WaveIn.DeviceCount;
    }

    public int GetOutputDeviceCount()
    {
        return WaveOut.DeviceCount;
    }

    public async Task<bool> RequestInputPermissions()
    {
        var status = await Permissions.RequestAsync<Permissions.Microphone>();
        if (Permissions.ShouldShowRationale<Permissions.Microphone>())
        {
            Shell.Current.DisplayAlert("Error", "VoiceCraft requires the microphone to communicate with other users!", "OK").Wait();
            return false;
        }

        return status == PermissionStatus.Granted;
    }
}