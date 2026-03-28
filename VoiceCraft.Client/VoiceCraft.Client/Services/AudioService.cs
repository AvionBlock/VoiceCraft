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
        foreach (var audioPreprocessor in registeredAudioPreprocessors)
            _registeredAudioPreprocessors.TryAdd(audioPreprocessor.Id, audioPreprocessor);
        foreach (var registeredClipper in registeredClippers)
            _registeredAudioClippers.TryAdd(registeredClipper.Id, registeredClipper);
    }

    public IEnumerable<RegisteredAudioPreprocessor> RegisteredAudioPreprocessors =>
        _registeredAudioPreprocessors.Values.ToArray();

    public IEnumerable<RegisteredAudioClipper> RegisteredAudioClippers => _registeredAudioClippers.Values.ToArray();

    public RegisteredAudioPreprocessor? GetAudioPreprocessor(Guid id)
    {
        return _registeredAudioPreprocessors.GetValueOrDefault(id);
    }

    public RegisteredAudioClipper? GetAudioClipper(Guid id)
    {
        return _registeredAudioClippers.GetValueOrDefault(id);
    }

    public IEnumerable<string> GetInputDevices()
    {
        _engine.UpdateAudioDevicesInfo();
        return _engine.CaptureDevices.Select(x => x.Name);
    }

    public IEnumerable<string> GetOutputDevices()
    {
        _engine.UpdateAudioDevicesInfo();
        return _engine.PlaybackDevices.Select(x => x.Name);
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
                ContentType = AAudioContentType.Speech,
                Usage = AAudioUsage.VoiceCommunication
            }
        };
        var device = _engine.CaptureDevices.FirstOrDefault(x => x.Name == inputDevice);
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
            }
        };
        var device = _engine.PlaybackDevices.FirstOrDefault(x => x.Name == outputDevice);
        return _engine.InitializePlaybackDevice(device, format, config);
    }
}

public class RegisteredAudioPreprocessor(Guid id, string name, Func<IAudioPreprocessor> factory)
{
    public Guid Id { get; } = id;
    public string Name { get; } = name;

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