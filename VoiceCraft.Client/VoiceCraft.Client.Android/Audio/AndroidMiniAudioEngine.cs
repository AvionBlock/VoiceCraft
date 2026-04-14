using System;
using System.Collections.Generic;
using Android.Media;
using SoundFlow.Abstracts;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Enums;
using SoundFlow.Structs;
using AudioFormat = SoundFlow.Structs.AudioFormat;

namespace VoiceCraft.Client.Android.Audio;

public class AndroidMiniAudioEngine : AudioEngine
{
    private static readonly NativeDataFormat[] DefaultFormats =
    [
        new() { Format = SampleFormat.F32, Channels = 1, SampleRate = 48_000, Flags = 0 },
        new() { Format = SampleFormat.F32, Channels = 2, SampleRate = 48_000, Flags = 0 }
    ];
    
    private static readonly DeviceInfo DefaultPlaybackDevice = new()
    {
        Id = 1,
        Name = "Android Default Output",
        IsDefault = true,
        SupportedDataFormats = DefaultFormats
    };

    private static readonly DeviceInfo DefaultCaptureDevice = new()
    {
        Id = 2,
        Name = "Android Default Input",
        IsDefault = true,
        SupportedDataFormats = DefaultFormats
    };
    
    private readonly AudioManager _audioManager;
    public AndroidMiniAudioEngine(AudioManager audioManager)
    {
        _audioManager = audioManager;
        UpdateAudioDevicesInfo();
    }

    protected override void CleanupBackend()
    {
    }

    public override AudioPlaybackDevice InitializePlaybackDevice(DeviceInfo? deviceInfo, AudioFormat format,
        DeviceConfig? config = null)
    {
        throw new System.NotImplementedException();
    }

    public override AudioCaptureDevice InitializeCaptureDevice(DeviceInfo? deviceInfo, AudioFormat format,
        DeviceConfig? config = null)
    {
        throw new System.NotImplementedException();
    }

    public override FullDuplexDevice InitializeFullDuplexDevice(DeviceInfo? playbackDeviceInfo,
        DeviceInfo? captureDeviceInfo,
        AudioFormat format, DeviceConfig? config = null)
    {
        throw new NotSupportedException("Full duplex mode is not supported by the Android audio engine.");
    }

    public override AudioCaptureDevice InitializeLoopbackDevice(AudioFormat format, DeviceConfig? config = null)
    {
        throw new NotSupportedException("Loopback capture is not supported on Android.");
    }

    public override AudioPlaybackDevice SwitchDevice(AudioPlaybackDevice oldDevice, DeviceInfo newDeviceInfo,
        DeviceConfig? config = null)
    {
        var shouldRun = oldDevice.IsRunning;
        oldDevice.Stop();
        oldDevice.Dispose();

        var next = InitializePlaybackDevice(newDeviceInfo, oldDevice.Format, config);
        if (shouldRun)
            next.Start();
        return next;
    }

    public override AudioCaptureDevice SwitchDevice(AudioCaptureDevice oldDevice, DeviceInfo newDeviceInfo,
        DeviceConfig? config = null)
    {
        var shouldRun = oldDevice.IsRunning;
        oldDevice.Stop();
        oldDevice.Dispose();

        var next = InitializeCaptureDevice(newDeviceInfo, oldDevice.Format, config);
        if (shouldRun)
            next.Start();
        return next;
    }

    public override FullDuplexDevice SwitchDevice(FullDuplexDevice oldDevice, DeviceInfo? newPlaybackInfo,
        DeviceInfo? newCaptureInfo, DeviceConfig? config = null)
    {
        throw new NotSupportedException("Full duplex mode is not supported by the Android audio engine.");
    }

    public sealed override void UpdateAudioDevicesInfo()
    {
        var androidPlaybackDevices = _audioManager.GetDevices(GetDevicesTargets.Outputs);
        if (androidPlaybackDevices == null) return;
        var playbackDevices = new List<DeviceInfo>() { DefaultPlaybackDevice };
        var captureDevices = new List<DeviceInfo>() { DefaultCaptureDevice };

        foreach (var device in androidPlaybackDevices)
        {
            playbackDevices.Add(new DeviceInfo()
            {
                SupportedDataFormats = [new NativeDataFormat() {  } ]
            });
        }
    }
}