using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Media;
using Android.OS;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;
using AudioFormat = VoiceCraft.Core.AudioFormat;

namespace VoiceCraft.Client.Android.Audio;

public class NativeAudioService : AudioService
{
    //This may or may not include bugged devices that can crash the application.
    private static readonly List<AudioDeviceType> DeniedDeviceTypes =
    [
        AudioDeviceType.Unknown,
        AudioDeviceType.Fm,
        AudioDeviceType.FmTuner,
        AudioDeviceType.TvTuner,
        AudioDeviceType.Telephony,
        AudioDeviceType.Ip
    ];

    private readonly AudioManager _audioManager;

    public NativeAudioService(AudioManager audioManager)
    {
        _audioManager = audioManager;

#pragma warning disable CA1416
        if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
            DeniedDeviceTypes.Add(AudioDeviceType.Bus);
        if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            DeniedDeviceTypes.Add(AudioDeviceType.HearingAid);
        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            DeniedDeviceTypes.Add(AudioDeviceType.BuiltinSpeakerSafe);
        if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
        {
            DeniedDeviceTypes.Add(AudioDeviceType.RemoteSubmix);
            DeniedDeviceTypes.Add(AudioDeviceType.HdmiEarc);
        }

        if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            DeniedDeviceTypes.Add(AudioDeviceType.RemoteSubmix);
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            DeniedDeviceTypes.Add(AudioDeviceType.BleBroadcast);
#pragma warning restore CA1416
    }

    public override IAudioRecorder CreateAudioRecorder(int sampleRate, int channels, AudioFormat format)
    {
        return new AudioRecorder(_audioManager, sampleRate, channels, format);
    }

    public override IAudioPlayer CreateAudioPlayer(int sampleRate, int channels, AudioFormat format)
    {
        return new AudioPlayer(_audioManager, sampleRate, channels, format);
    }

    public override Task<List<string>> GetInputDevicesAsync()
    {
        var devices = new List<string>();

        var audioDevices =
            _audioManager.GetDevices(GetDevicesTargets.Inputs)
                ?.Where(x => !DeniedDeviceTypes
                    .Contains(x.Type)); //Don't ask. this is the only way to stop users from selecting a device that completely annihilates the app.
        if (audioDevices == null) return Task.FromResult(devices);

        foreach (var audioDevice in audioDevices)
        {
            var deviceName = $"{audioDevice.ProductName.Truncate(8)} - {audioDevice.Type}";
            if (!devices.Contains(deviceName))
                devices.Add(deviceName);
        }

        return Task.FromResult(devices);
    }

    public override Task<List<string>> GetOutputDevicesAsync()
    {
        var devices = new List<string>();

        var audioDevices =
            _audioManager.GetDevices(GetDevicesTargets.Outputs)
                ?.Where(x => !DeniedDeviceTypes
                    .Contains(x.Type)); //Don't ask. this is the only way to stop users from selecting a device that completely annihilates the app.
        if (audioDevices == null) return Task.FromResult(devices);

        foreach (var audioDevice in audioDevices)
        {
            var deviceName = $"{audioDevice.ProductName.Truncate(8)} - {audioDevice.Type}";
            if (!devices.Contains(deviceName))
                devices.Add(deviceName);
        }

        return Task.FromResult(devices);
    }
}