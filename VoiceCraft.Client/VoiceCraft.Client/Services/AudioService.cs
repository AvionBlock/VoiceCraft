using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using VoiceCraft.Client.Audio.Interfaces;

namespace VoiceCraft.Client.Services
{
    public abstract class AudioService
    {
        protected readonly ConcurrentDictionary<Guid, RegisteredEchoCanceler> _registeredEchoCancelers;
        protected readonly ConcurrentDictionary<Guid, RegisteredPreprocessor> _registeredPreprocessors;

        public AudioService()
        {
            _registeredEchoCancelers = new ConcurrentDictionary<Guid, RegisteredEchoCanceler>();
            _registeredPreprocessors = new ConcurrentDictionary<Guid, RegisteredPreprocessor>();
        }

        public void RegisterEchoCanceler(string name, Type echoCanceler)
        {
            if (!typeof(IEchoCanceler).IsAssignableFrom(echoCanceler)) throw new ArgumentException($"Echo canceler must implement {nameof(IEchoCanceler)}.", nameof(echoCanceler));
            _registeredEchoCancelers.AddOrUpdate(name, echoCanceler, (key, old) => old = echoCanceler);
        }

        public void RegisterPreprocessor(string name, Type preprocessor)
        {
            if (!typeof(IPreprocessor).IsAssignableFrom(preprocessor)) throw new ArgumentException($"Echo canceler must implement {nameof(IPreprocessor)}.", nameof(preprocessor));
            _registeredPreprocessors.AddOrUpdate(name, preprocessor, (key, old) => old = preprocessor);
        }

        public void UnregisterEchoCanceler(string name)
        {
            _registeredEchoCancelers.TryRemove(name, out _);
        }

        public void UnregisterPreprocessor(string name)
        {
            _registeredPreprocessors.TryRemove(name, out _);
        }

        public IEchoCanceler? CreateEchoCanceler(string name)
        {
            if (_registeredEchoCancelers.TryGetValue(name, out var echoCanceler))
            {
                return (IEchoCanceler?)Activator.CreateInstance(echoCanceler);
            }
            return null;
        }

        public IPreprocessor? CreatePreprocessor(string name)
        {
            if (_registeredPreprocessors.TryGetValue(name, out var preprocessor))
            {
                return (IPreprocessor?)Activator.CreateInstance(preprocessor);
            }
            return null;
        }

        public abstract string GetDefaultInputDevice();

        public abstract string GetDefaultOutputDevice();

        public abstract string GetDefaultPreprocessor();

        public abstract string GetDefaultEchoCanceler();

        public abstract List<string> GetInputDevices();

        public abstract List<string> GetOutputDevices();

        public abstract List<string> GetPreprocessors();

        public abstract List<string> GetEchoCancelers();

        public abstract IAudioRecorder CreateAudioRecorder();

        public abstract IAudioPlayer CreateAudioPlayer();
    }

    public class RegisteredEchoCanceler(Guid id, string name, Type type)
    {
        public readonly Guid Id = id;
        public readonly string Name = name;
        public readonly Type Type = type;
    }

    public class RegisteredPreprocessor(Guid id, string name, Type type)
    {
        public readonly Guid Id = id;
        public readonly string Name = name;
        public readonly Type Type = type;
    }
}
