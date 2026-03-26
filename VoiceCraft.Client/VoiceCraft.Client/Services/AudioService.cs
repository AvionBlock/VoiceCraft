using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Ownaudio.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Services;

public class AudioService
{
    private readonly ConcurrentDictionary<Guid, RegisteredAudioPreprocessor> _registeredAudioPreprocessors = new();
    private readonly ConcurrentDictionary<Guid, RegisteredAudioClipper> _registeredAudioClippers = new();

    protected AudioService(
        IEnumerable<RegisteredAudioPreprocessor> registeredAudioPreprocessors,
        IEnumerable<RegisteredAudioClipper> registeredClippers)
    {
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

    public static IEnumerable<string> GetInputDevices()
    {
        using var engine = AudioEngineFactory.CreateDefault();
        engine.Initialize(AudioConfig.Default);
        return engine.GetInputDevices().Select(x => x.Name);
    }

    public static IEnumerable<string> GetOutputDevices()
    {
        using var engine = AudioEngineFactory.CreateDefault();
        engine.Initialize(AudioConfig.Default);
        return engine.GetOutputDevices().Select(x => x.Name);
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