using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Services;

public abstract class AudioService
{
    private readonly ConcurrentDictionary<Guid, RegisteredAutomaticGainController> _registeredAutomaticGainControllers = new();
    private readonly ConcurrentDictionary<Guid, RegisteredEchoCanceler> _registeredEchoCancelers = new();
    private readonly ConcurrentDictionary<Guid, RegisteredDenoiser> _registeredDenoisers = new();

    protected AudioService(
        IEnumerable<RegisteredAutomaticGainController> registeredAutomaticGainControllers,
        IEnumerable<RegisteredEchoCanceler> registeredEchoCancelers,
        IEnumerable<RegisteredDenoiser> registeredDenoisers)
    {
        _registeredAutomaticGainControllers.TryAdd(Guid.Empty, new RegisteredAutomaticGainController(Guid.Empty, "None", null));
        _registeredEchoCancelers.TryAdd(Guid.Empty, new RegisteredEchoCanceler(Guid.Empty, "None", null));
        _registeredDenoisers.TryAdd(Guid.Empty, new RegisteredDenoiser(Guid.Empty, "None", null));

        foreach (var registeredAutomaticGainController in registeredAutomaticGainControllers)
        {
            _registeredAutomaticGainControllers.TryAdd(registeredAutomaticGainController.Id, registeredAutomaticGainController);
        }

        foreach (var registeredEchoCanceler in registeredEchoCancelers)
        {
            _registeredEchoCancelers.TryAdd(registeredEchoCanceler.Id, registeredEchoCanceler);
        }
        
        foreach (var registeredDenoiser in registeredDenoisers)
        {
            _registeredDenoisers.TryAdd(registeredDenoiser.Id, registeredDenoiser);
        }
    }

    public IEnumerable<RegisteredDenoiser> RegisteredDenoisers => _registeredDenoisers.Values.ToArray();
    public IEnumerable<RegisteredAutomaticGainController> RegisteredAutomaticGainControllers => _registeredAutomaticGainControllers.Values.ToArray();
    public IEnumerable<RegisteredEchoCanceler> RegisteredEchoCancelers => _registeredEchoCancelers.Values.ToArray();

    public RegisteredDenoiser? GetDenoiser(Guid id)
    {
        return _registeredDenoisers.GetValueOrDefault(id);
    }

    public RegisteredAutomaticGainController? GetAutomaticGainController(Guid id)
    {
        return _registeredAutomaticGainControllers.GetValueOrDefault(id);
    }

    public RegisteredEchoCanceler? GetEchoCanceler(Guid id)
    {
        return _registeredEchoCancelers.GetValueOrDefault(id);
    }

    public abstract Task<List<string>> GetInputDevicesAsync();

    public abstract Task<List<string>> GetOutputDevicesAsync();

    public abstract IAudioRecorder CreateAudioRecorder(int sampleRate, int channels, AudioFormat format);

    public abstract IAudioPlayer CreateAudioPlayer(int sampleRate, int channels, AudioFormat format);
}

public class RegisteredEchoCanceler(Guid id, string name, Type? type)
{
    public Guid Id { get; } = id;
    public string Name { get; } = name;

    public IEchoCanceler? Instantiate()
    {
        //IL 2077 warning here.
        if (type != null)
#pragma warning disable IL2067
            return Activator.CreateInstance(type) as IEchoCanceler;
#pragma warning restore IL2067
        return null;
    }
}

public class RegisteredAutomaticGainController(Guid id, string name, Type? type)
{
    public Guid Id { get; } = id;
    public string Name { get; } = name;

    public IAutomaticGainController? Instantiate()
    {
        if (type != null)
#pragma warning disable IL2067
            return Activator.CreateInstance(type) as IAutomaticGainController;
#pragma warning restore IL2067
        return null;
    }
}

public class RegisteredDenoiser(Guid id, string name, Type? type)
{
    public Guid Id { get; } = id;
    public string Name { get; } = name;

    public IDenoiser? Instantiate()
    {
        //IL 2077 warning here.
        if (type != null)
#pragma warning disable IL2067
            return Activator.CreateInstance(type) as IDenoiser;
#pragma warning restore IL2067
        return null;
    }
}