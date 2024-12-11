using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using VoiceCraft.Client.Audio.Interfaces;

namespace VoiceCraft.Client.Services
{
    public abstract class AudioService
    {
        public IEnumerable<RegisteredPreprocessor> RegisteredPreprocessors => _registeredPreprocessors.Values.ToArray();
        public IEnumerable<RegisteredEchoCanceler> RegisteredEchoCancelers => _registeredEchoCancelers.Values.ToArray();

        private readonly ConcurrentDictionary<Guid, RegisteredEchoCanceler> _registeredEchoCancelers = new();
        private readonly ConcurrentDictionary<Guid, RegisteredPreprocessor> _registeredPreprocessors = new();

        protected AudioService()
        {
            _registeredEchoCancelers.TryAdd(Guid.Empty, new RegisteredEchoCanceler(Guid.Empty, "None", null, false));
            _registeredPreprocessors.TryAdd(Guid.Empty,
                new RegisteredPreprocessor(Guid.Empty, "None", null, false, false, false));
        }
        
        public bool RegisterPreprocessor<T>(Guid id, string name, bool supportsGainController,
            bool supportsNoiseSuppressor,
            bool supportsVoiceActivity) where T : IPreprocessor
        {
            return _registeredPreprocessors.TryAdd(id,
                new RegisteredPreprocessor(id, name, typeof(T), supportsGainController, supportsNoiseSuppressor,
                    supportsVoiceActivity));
        }

        public bool RegisterEchoCanceler<T>(Guid id, string name, bool available) where T : IEchoCanceler
        {
            return _registeredEchoCancelers.TryAdd(id, new RegisteredEchoCanceler(id, name, typeof(T), available));
        }
        
        public bool UnregisterPreprocessor(Guid id)
        {
            return _registeredPreprocessors.TryRemove(id, out _);
        }

        public bool UnregisterEchoCanceler(Guid id)
        {
            return _registeredEchoCancelers.TryRemove(id, out _);
        }

        public RegisteredPreprocessor GetPreprocessor(Guid id)
        {
            return !_registeredPreprocessors.TryGetValue(id, out var preprocessor) ? GetDefaultPreprocessor() : preprocessor;
        }
        
        public RegisteredEchoCanceler GetEchoCanceler(Guid id)
        {
            return !_registeredEchoCancelers.TryGetValue(id, out var echoCanceler) ? GetDefaultEchoCanceler() : echoCanceler;
        }
        
        public IPreprocessor? CreatePreprocessor(Guid id)
        {
            if (!_registeredPreprocessors.TryGetValue(id, out var preprocessor)) return null;
            if (preprocessor.Type == null) return null;
            return (IPreprocessor?)Activator.CreateInstance(preprocessor.Type);
        }

        public IEchoCanceler? CreateEchoCanceler(Guid id)
        {
            if (!_registeredEchoCancelers.TryGetValue(id, out var echoCanceler)) return null;
            if (echoCanceler.Type == null) return null;
            return (IEchoCanceler?)Activator.CreateInstance(echoCanceler.Type);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public RegisteredPreprocessor GetDefaultPreprocessor()
        {
            return _registeredPreprocessors[Guid.Empty];
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

    public class RegisteredEchoCanceler(Guid id, string name, Type? type, bool isAvailable)
    {
        public Guid Id { get; } = id;
        public string Name { get; } = name;
        public Type? Type { get; } = type;
        public bool IsAvailable { get; } = isAvailable;
    }

    public class RegisteredPreprocessor(
        Guid id,
        string name,
        Type? type,
        bool supportsGainController,
        bool supportsNoiseSuppressor,
        bool supportsVoiceActivity)
    {
        public Guid Id { get; } = id;
        public string Name { get; } = name;
        public Type? Type { get; } = type;
        public bool IsGainControllerAvailable { get; } = supportsGainController;
        public bool IsNoiseSuppressorAvailable { get; } = supportsNoiseSuppressor;
        public bool IsVoiceActivityDetectionAvailable { get; } = supportsVoiceActivity;
    }
}