using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;

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

        public bool RegisterAutomaticGainController<T>(Guid id, string name) where T : IAutomaticGainController
        {
            return _registeredAutomaticGainControllers.TryAdd(id, new RegisteredAutomaticGainController(id, name, typeof(T)));
        }

        public bool RegisterDenoiser<T>(Guid id, string name) where T : IDenoiser
        {
            return _registeredDenoisers.TryAdd(id, new RegisteredDenoiser(id, name, typeof(T)));
        }

        public bool UnregisterEchoCanceler(Guid id)
        {
            return _registeredEchoCancelers.TryRemove(id, out _);
        }

        public bool UnregisterAutomaticGainController(Guid id)
        {
            return _registeredAutomaticGainControllers.TryRemove(id, out _);
        }

        public bool UnregisterDenoiser(Guid id)
        {
            return _registeredDenoisers.TryRemove(id, out _);
        }

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

        public abstract List<string> GetInputDevices();

        public abstract List<string> GetOutputDevices();

        public abstract IAudioRecorder CreateAudioRecorder(int sampleRate, int channels, AudioFormat format);

        public abstract IAudioPlayer CreateAudioPlayer(int sampleRate, int channels, AudioFormat format);
    }

    public class RegisteredEchoCanceler(Guid id, string name, Type? type)
    {
        public Guid Id { get; } = id;
        public string Name { get; } = name;

        public IEchoCanceler? Instantiate()
        {
            if (type != null)
                return Activator.CreateInstance(type) as IEchoCanceler;
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
                return Activator.CreateInstance(type) as IAutomaticGainController;
            return null;
        }
    }

    public class RegisteredDenoiser(Guid id, string name, Type? type)
    {
        public Guid Id { get; } = id;
        public string Name { get; } = name;

        public IDenoiser? Instantiate()
        {
            if (type != null)
                return Activator.CreateInstance(type) as IDenoiser;
            return null;
        }
    }
}