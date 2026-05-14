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
using VoiceCraft.Client.Audio;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Services;

public delegate void AudioCaptureFrameHandler(Span<float> buffer);

public interface IVoiceCraftAudioService
{
    IEnumerable<RegisteredAudioPreprocessor> RegisteredAudioPreprocessors { get; }
    IEnumerable<RegisteredAudioClipper> RegisteredAudioClippers { get; }
    RegisteredAudioPreprocessor? GetAudioPreprocessor(Guid id);
    RegisteredAudioClipper? GetAudioClipper(Guid id);
    IEnumerable<AudioDeviceInfo> GetInputDevices();
    IEnumerable<AudioDeviceInfo> GetOutputDevices();
    IAudioCaptureSession InitializeCaptureSession(
        int sampleRate,
        int channels,
        uint frameSize,
        string inputDevice,
        bool hardwarePreprocessorsEnabled);
    IAudioPlaybackSession InitializePlaybackSession(
        int sampleRate,
        int channels,
        uint frameSize,
        string outputDevice,
        Func<Span<float>, int> read);
    IAudioPlaybackSession InitializeTonePlaybackSession(
        int sampleRate,
        int channels,
        uint frameSize,
        string outputDevice,
        Func<Span<float>, int> read);
}

public interface IAudioCaptureSession : IDisposable
{
    bool IsRunning { get; }
    event AudioCaptureFrameHandler? OnAudioProcessed;
    void Start();
    void Stop();
}

public interface IAudioPlaybackSession : IDisposable
{
    bool IsRunning { get; }
    void Start();
    void Stop();
    void Pump();
    void PlayTone(TimeSpan duration, float frequency);
}

public class AudioService : IVoiceCraftAudioService
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
        var devices = new List<AudioDeviceInfo>();
        AudioDeviceInfo? defaultDevice = null;
        foreach (var device in _engine.CaptureDevices)
        {
            if (device.IsDefault)
            {
                defaultDevice = new AudioDeviceInfo(
                    "Default",
                    $"AudioService.AudioDeviceInfo.DefaultDevice:{device.Name}",
                    true);
            }

            devices.Add(new AudioDeviceInfo(device.Name, device.Name, false));
        }

        devices.Insert(0,
            defaultDevice ??
            new AudioDeviceInfo("Default", "AudioService.AudioDeviceInfo.Default", true));
        return devices;
    }

    public IEnumerable<AudioDeviceInfo> GetOutputDevices()
    {
        _engine.UpdateAudioDevicesInfo();
        var devices = new List<AudioDeviceInfo>();
        AudioDeviceInfo? defaultDevice = null;
        foreach (var device in _engine.PlaybackDevices)
        {
            if (device.IsDefault)
            {
                defaultDevice = new AudioDeviceInfo(
                    "Default",
                    $"AudioService.AudioDeviceInfo.DefaultDevice:{device.Name}",
                    true);
            }

            devices.Add(new AudioDeviceInfo(device.Name, device.Name, false));
        }

        devices.Insert(0,
            defaultDevice ??
            new AudioDeviceInfo("Default", "AudioService.AudioDeviceInfo.Default", true));
        return devices;
    }

    public AudioCaptureDevice InitializeCaptureDevice(
        int sampleRate,
        int channels,
        uint frameSize,
        string inputDevice,
        bool hardwarePreprocessorsEnabled)
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
                InputPreset = hardwarePreprocessorsEnabled
                    ? AAudioInputPreset.VoiceCommunication
                    : AAudioInputPreset.Unprocessed
            },
            OpenSL = new OpenSlSettings()
            {
                RecordingPreset = hardwarePreprocessorsEnabled
                    ? OpenSlRecordingPreset.VoiceCommunication
                    : OpenSlRecordingPreset.VoiceUnprocessed
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

    public IAudioCaptureSession InitializeCaptureSession(
        int sampleRate,
        int channels,
        uint frameSize,
        string inputDevice,
        bool hardwarePreprocessorsEnabled)
    {
        return new SoundFlowCaptureSession(InitializeCaptureDevice(
            sampleRate,
            channels,
            frameSize,
            inputDevice,
            hardwarePreprocessorsEnabled));
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

    public IAudioPlaybackSession InitializePlaybackSession(
        int sampleRate,
        int channels,
        uint frameSize,
        string outputDevice,
        Func<Span<float>, int> read)
    {
        var device = InitializePlaybackDevice(sampleRate, channels, frameSize, outputDevice);
        var callbackComponent = new CallbackProvider(device.Engine, device.Format, read);
        device.MasterMixer.AddComponent(callbackComponent);
        return new SoundFlowPlaybackSession(device);
    }

    public IAudioPlaybackSession InitializeTonePlaybackSession(
        int sampleRate,
        int channels,
        uint frameSize,
        string outputDevice,
        Func<Span<float>, int> read)
    {
        var device = InitializePlaybackDevice(sampleRate, channels, frameSize, outputDevice);
        var callbackComponent = new CallbackProvider(device.Engine, device.Format, read);
        callbackComponent.ConnectInput(new SoundFlow.Components.Oscillator(device.Engine, device.Format));
        device.MasterMixer.AddComponent(callbackComponent);
        return new SoundFlowPlaybackSession(device);
    }

    private sealed class SoundFlowCaptureSession(AudioCaptureDevice device) : IAudioCaptureSession
    {
        public bool IsRunning => device.IsRunning;
        public event AudioCaptureFrameHandler? OnAudioProcessed;

        public void Start()
        {
            device.OnAudioProcessed += DeviceOnAudioProcessed;
            device.Start();
        }

        public void Stop()
        {
            device.OnAudioProcessed -= DeviceOnAudioProcessed;
            device.Stop();
        }

        public void Dispose()
        {
            Stop();
            device.Dispose();
        }

        private void DeviceOnAudioProcessed(Span<float> buffer, Capability _)
        {
            OnAudioProcessed?.Invoke(buffer);
        }
    }

    private sealed class SoundFlowPlaybackSession(AudioPlaybackDevice device) : IAudioPlaybackSession
    {
        private readonly ToneProvider _toneProvider = new(device.Engine, device.Format);

        public bool IsRunning => device.IsRunning;

        public void Start()
        {
            device.MasterMixer.AddComponent(_toneProvider);
            device.Start();
        }

        public void Stop()
        {
            device.Stop();
        }

        public void Pump()
        {
        }

        public void PlayTone(TimeSpan duration, float frequency)
        {
            _toneProvider.Play(duration, frequency);
        }

        public void Dispose()
        {
            Stop();
            device.Dispose();
        }
    }

    private class EmptyRegisteredAudioPreprocessor()
        : RegisteredAudioPreprocessor(Guid.Empty, "AudioService.Clippers.None", () => throw new NotSupportedException(),
            true, true, true);

    private class EmptyRegisteredAudioClipper()
        : RegisteredAudioClipper(Guid.Empty, "AudioService.Clippers.None", () => throw new NotSupportedException());
}

public class AudioDeviceInfo(string name, string displayName, bool isDefault)
{
    public string DisplayName { get; } = displayName;
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
