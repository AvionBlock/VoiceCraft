using NAudio.Wave;
using VoiceCraft.Maui.Interfaces;

namespace VoiceCraft.Maui;

public class AudioManager : IAudioManager
{
    public static AudioManager Instance { get; } = new AudioManager(new Models.SettingsModel());
    
    private readonly Models.SettingsModel _settings;
    
    public AudioManager(Models.SettingsModel settings)
    {
        _settings = settings;
    }

    public IWavePlayer CreatePlayer(ISampleProvider waveProvider)
    {
        var Player = new AudioTrackOut
        {
            DesiredLatency = 50,
            NumberOfBuffers = 3
        };
        Player.Init(waveProvider);
        return Player;
    }

    public IWaveIn CreateRecorder(WaveFormat waveFormat, int bufferMS)
    {
        var Recorder = new AudioRecorder
        {
            WaveFormat = waveFormat,
            BufferMilliseconds = bufferMS,
            audioSource = Android.Media.AudioSource.VoiceCommunication
        };
        return Recorder;
    }

    public string[] GetInputDevices()
    {
        return [];
    }

    public string[] GetOutputDevices()
    {
        var outputDevices = new List<string>()
        {
            "Phone",
            "Speaker"
        };
        return [.. outputDevices];
    }

    public int GetInputDeviceCount()
    {
        return 0;
    }

    public int GetOutputDeviceCount()
    {
        return 2;
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