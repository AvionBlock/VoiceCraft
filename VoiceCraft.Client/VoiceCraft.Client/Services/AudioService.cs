using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SoundFlow.Abstracts;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio.Devices;
using SoundFlow.Backends.MiniAudio.Enums;
using SoundFlow.Enums;
using SoundFlow.Structs;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.Services;

public class AudioService
{
    private readonly AudioEngine _engine;

    private readonly ConcurrentDictionary<Guid, RegisteredAudioPreprocessor> _registeredAudioPreprocessors = new();
    private readonly ConcurrentDictionary<Guid, RegisteredAudioClipper> _registeredAudioClippers = new();

    public AudioService(
        AudioEngine engine,
        IEnumerable<RegisteredAudioPreprocessor> registeredAudioPreprocessors,
        IEnumerable<RegisteredAudioClipper> registeredClippers)
    {
        _engine = engine;
        _registeredAudioPreprocessors.TryAdd(Guid.Empty, new EmptyRegisteredAudioPreprocessor());
        _registeredAudioClippers.TryAdd(Guid.Empty, new EmptyRegisteredAudioClipper());

        foreach (var audioPreprocessor in registeredAudioPreprocessors)
            _registeredAudioPreprocessors.TryAdd(audioPreprocessor.Id, audioPreprocessor);
        foreach (var registeredClipper in registeredClippers)
            _registeredAudioClippers.TryAdd(registeredClipper.Id, registeredClipper);
    }

    public IEnumerable<RegisteredAudioPreprocessor> RegisteredAudioPreprocessors =>
        _registeredAudioPreprocessors.Values.ToArray();

    public IEnumerable<RegisteredAudioClipper> RegisteredAudioClippers =>
        _registeredAudioClippers.Values.ToArray();

    public RegisteredAudioPreprocessor? GetAudioPreprocessor(Guid id)
    {
        return id == Guid.Empty ? null : _registeredAudioPreprocessors.GetValueOrDefault(id);
    }

    public RegisteredAudioClipper? GetAudioClipper(Guid id)
    {
        return id == Guid.Empty ? null : _registeredAudioClippers.GetValueOrDefault(id);
    }

    public IEnumerable<AudioDeviceInfo> GetInputDevices()
    {
        _engine.UpdateAudioDevicesInfo();
        return _engine.CaptureDevices.Select(x => new AudioDeviceInfo(x.Name, x.IsDefault));
    }

    public IEnumerable<AudioDeviceInfo> GetOutputDevices()
    {
        _engine.UpdateAudioDevicesInfo();
        return _engine.PlaybackDevices.Select(x => new AudioDeviceInfo(x.Name, x.IsDefault));
    }

    public AudioCaptureDevice InitializeCaptureDevice(int sampleRate, int channels, uint frameSize, string inputDevice)
    {
        _engine.UpdateAudioDevicesInfo();
        var format = new AudioFormat()
        {
            SampleRate = sampleRate,
            Channels = channels,
            Format = SampleFormat.F32
        };
        var config = new MiniAudioDeviceConfig()
        {
            PeriodSizeInFrames = frameSize,
            AAudio = new AAudioSettings()
            {
                Usage = AAudioUsage.VoiceCommunication,
                InputPreset = AAudioInputPreset.VoiceCommunication
            },
            OpenSL = new OpenSlSettings()
            {
                RecordingPreset = OpenSlRecordingPreset.VoiceCommunication
            },
            CoreAudio = new CoreAudioSettings()
            {
                AllowNominalSampleRateChange = true
            }
        };

        var device = _engine.CaptureDevices.FirstOrDefault(x => x.Name == inputDevice);
        if (device.Id == nint.Zero)
            device = _engine.CaptureDevices.FirstOrDefault(x => x.IsDefault);

        return _engine.InitializeCaptureDevice(device, format, config);
    }

    public AudioPlaybackDevice InitializePlaybackDevice(
        int sampleRate,
        int channels,
        uint frameSize,
        string outputDevice)
    {
        _engine.UpdateAudioDevicesInfo();
        var format = new AudioFormat()
        {
            SampleRate = sampleRate,
            Channels = channels,
            Format = SampleFormat.F32
        };
        var config = new MiniAudioDeviceConfig()
        {
            PeriodSizeInFrames = frameSize,
            AAudio = new AAudioSettings()
            {
                ContentType = AAudioContentType.Music,
                Usage = AAudioUsage.Media
            },
            OpenSL = new OpenSlSettings()
            {
                StreamType = OpenSlStreamType.Media
            },
            CoreAudio = new CoreAudioSettings()
            {
                AllowNominalSampleRateChange = true
            }
        };

        var device = _engine.PlaybackDevices.FirstOrDefault(x => x.Name == outputDevice);
        if (device.Id == nint.Zero)
            device = _engine.PlaybackDevices.FirstOrDefault(x => x.IsDefault);

        return _engine.InitializePlaybackDevice(device, format, config);
    }

    private class EmptyRegisteredAudioPreprocessor()
        : RegisteredAudioPreprocessor(Guid.Empty, "AudioService.Clippers.None", () => throw new NotSupportedException(),
            true, true, true);

    private class EmptyRegisteredAudioClipper()
        : RegisteredAudioClipper(Guid.Empty, "AudioService.Clippers.None", () => throw new NotSupportedException());
}

public class AudioDeviceInfo(string name, bool isDefault)
{
    public string DisplayName => IsDefault ? Localizer.Get($"AudioService.AudioDeviceInfo.Default:{Name}") : Name;
    public string Name { get; } = name;
    public bool IsDefault { get; } = isDefault;
}

public class RegisteredAudioPreprocessor(
    Guid id,
    string name,
    Func<IAudioPreprocessor> factory,
    bool denoiserSupported,
    bool gainControllerSupported,
    bool echoCancelerSupported)
{
    public Guid Id { get; } = id;
    public string Name { get; } = name;
    public bool DenoiserSupported { get; } = denoiserSupported;
    public bool GainControllerSupported { get; } = gainControllerSupported;
    public bool EchoCancelerSupported { get; } = echoCancelerSupported;

    public IAudioPreprocessor Instantiate()
    {
        return factory.Invoke();
    }
}

public class RegisteredAudioClipper(Guid id, string name, Func<IAudioClipper> factory)
{
    public Guid Id { get; } = id;
    public string Name { get; } = name;

    public IAudioClipper Instantiate()
    {
        return factory.Invoke();
    }
}