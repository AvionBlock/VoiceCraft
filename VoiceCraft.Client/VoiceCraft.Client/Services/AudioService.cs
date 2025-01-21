using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using VoiceCraft.Client.Audio.Interfaces;

namespace VoiceCraft.Client.Services
{
    public abstract class AudioService
    {
        public IEnumerable<RegisteredDenoiser> RegisteredDenoisers => _registeredDenoisers.Values.ToArray();
        public IEnumerable<RegisteredAutomaticGainController> RegisteredAutomaticGainControllers => _registeredAutomaticGainControllers.Values.ToArray();
        public IEnumerable<RegisteredEchoCanceler> RegisteredEchoCancelers => _registeredEchoCancelers.Values.ToArray();
        
        private readonly ConcurrentDictionary<Guid, RegisteredEchoCanceler> _registeredEchoCancelers = new();
        private readonly ConcurrentDictionary<Guid, RegisteredAutomaticGainController> _registeredAutomaticGainControllers = new();
        private readonly ConcurrentDictionary<Guid, RegisteredDenoiser> _registeredDenoisers = new();

        protected AudioService()
        {
            _registeredDenoisers.TryAdd(Guid.Empty, new RegisteredDenoiser(Guid.Empty, "None", null));
            _registeredAutomaticGainControllers.TryAdd(Guid.Empty, new RegisteredAutomaticGainController(Guid.Empty, "None", null));
            _registeredEchoCancelers.TryAdd(Guid.Empty, new RegisteredEchoCanceler(Guid.Empty, "None", null));
        }

        public bool RegisterEchoCanceler<T>(Guid id, string name) where T : IEchoCanceler
        {
            return _registeredEchoCancelers.TryAdd(id, new RegisteredEchoCanceler(id, name, typeof(T)));
        }

        public bool UnregisterEchoCanceler(Guid id)
        {
            return _registeredEchoCancelers.TryRemove(id, out _);
        }
        
        public RegisteredEchoCanceler GetEchoCanceler(Guid id)
        {
            return !_registeredEchoCancelers.TryGetValue(id, out var echoCanceler) ? GetDefaultEchoCanceler() : echoCanceler;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public RegisteredEchoCanceler GetDefaultEchoCanceler()
        {
            return _registeredEchoCancelers[Guid.Empty];
        }

        public abstract List<string> GetInputDevices();

        public abstract List<string> GetOutputDevices();

        public abstract IAudioRecorder CreateAudioRecorder();

        public abstract IAudioPlayer CreateAudioPlayer();
    }

    public class RegisteredEchoCanceler(Guid id, string name, Type? type)
    {
        public Guid Id { get; } = id;
        public string Name { get; } = name;
        public Type? Type { get; } = type;
    }
    
    public class RegisteredAutomaticGainController(Guid id, string name, Type? type)
    {
        public Guid Id { get; } = id;
        public string Name { get; } = name;
        public Type? Type { get; } = type;
    }

    public class RegisteredDenoiser(Guid id, string name, Type? type)
    {
        public Guid Id { get; } = id;
        public string Name { get; } = name;
        public Type? Type { get; } = type;
    }
}