using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using VoiceCraft.Client.Audio.Interfaces;

namespace VoiceCraft.Client.Services
{
    public abstract class AudioService
    {
        public IEnumerable<RegisteredEchoCanceler> RegisteredEchoCancelers => _registeredEchoCancelers.Values.ToArray();
        public IEnumerable<RegisteredPreprocessor> RegisteredPreprocessors => _registeredPreprocessors.Values.ToArray();
        
        private readonly ConcurrentDictionary<Guid, RegisteredEchoCanceler> _registeredEchoCancelers = new();
        private readonly ConcurrentDictionary<Guid, RegisteredPreprocessor> _registeredPreprocessors = new();

        protected AudioService()
        {
            _registeredEchoCancelers.TryAdd(Guid.Empty, new RegisteredEchoCanceler(Guid.Empty, "None", null));
            _registeredPreprocessors.TryAdd(Guid.Empty, new RegisteredPreprocessor(Guid.Empty, "None", null));
        }
        
        public bool RegisterEchoCanceler<T>(Guid id, string name) where T : IEchoCanceler
        {
            return _registeredEchoCancelers.TryAdd(id, new RegisteredEchoCanceler(id, name, typeof(T)));
        }

        public bool RegisterPreprocessor<T>(Guid id, string name) where T : IPreprocessor
        {
            return _registeredPreprocessors.TryAdd(id, new RegisteredPreprocessor(id, name, typeof(T)));
        }

        public bool UnregisterEchoCanceler(Guid id)
        {
            return _registeredEchoCancelers.TryRemove(id, out _);
        }

        public bool UnregisterPreprocessor(Guid id)
        {
            return _registeredPreprocessors.TryRemove(id, out _);
        }

        public IEchoCanceler? CreateEchoCanceler(Guid id)
        {
            if (!_registeredEchoCancelers.TryGetValue(id, out var echoCanceler)) return null;
            if (echoCanceler.Type == null) return null;
            return (IEchoCanceler?)Activator.CreateInstance(echoCanceler.Type);
        }

        public IPreprocessor? CreatePreprocessor(Guid id)
        {
            if (!_registeredPreprocessors.TryGetValue(id, out var preprocessor)) return null;
            if (preprocessor.Type == null) return null;
            return (IPreprocessor?)Activator.CreateInstance(preprocessor.Type);
        }

        public RegisteredPreprocessor GetDefaultPreprocessor()
        {
            return _registeredPreprocessors[Guid.Empty];
        }

        public RegisteredEchoCanceler GetDefaultEchoCanceler()
        {
            return _registeredEchoCancelers[Guid.Empty];
        }

        public abstract string GetDefaultInputDevice();
        
        public abstract string GetDefaultOutputDevice();

        public abstract List<string> GetInputDevices();

        public abstract List<string> GetOutputDevices();

        public abstract List<string> GetPreprocessors();

        public abstract List<string> GetEchoCancelers();

        public abstract IAudioRecorder CreateAudioRecorder();

        public abstract IAudioPlayer CreateAudioPlayer();
    }

    public class RegisteredEchoCanceler(Guid id, string name, Type? type)
    {
        public readonly Guid Id = id;
        public readonly string Name = name;
        public readonly Type? Type = type;
    }

    public class RegisteredPreprocessor(Guid id, string name, Type? type)
    {
        public readonly Guid Id = id;
        public readonly string Name = name;
        public readonly Type? Type = type;
    }
}