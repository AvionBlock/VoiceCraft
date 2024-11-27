using System.Collections.Concurrent;
using VoiceCraft.Client.PDK.Audio;

namespace VoiceCraft.Client.PDK.Services
{
    public abstract class AudioService
    {
        protected readonly ConcurrentDictionary<string, Type> _registeredEchoCancellers;
        protected readonly ConcurrentDictionary<string, Type> _registeredPreprocessors;

        public AudioService()
        {
            _registeredPreprocessors = new ConcurrentDictionary<string, Type>();
            _registeredEchoCancellers = new ConcurrentDictionary<string, Type>();
        }

        public void RegisterEchoCanceller(string name, Type echoCanceller)
        {
            if(!typeof(IEchoCanceller).IsAssignableFrom(echoCanceller)) throw new ArgumentException($"Echo canceller must implement {nameof(IEchoCanceller)}.", nameof(echoCanceller));
            _registeredEchoCancellers.AddOrUpdate(name, echoCanceller, (key, old) => old = echoCanceller);
        }

        public void RegisterPreprocessor(string name, Type preprocessor)
        {
            if (!typeof(IPreprocessor).IsAssignableFrom(preprocessor)) throw new ArgumentException($"Echo canceller must implement {nameof(IPreprocessor)}.", nameof(preprocessor));
            _registeredPreprocessors.AddOrUpdate(name, preprocessor, (key, old) => old = preprocessor);
        }

        public void UnregisterEchoCanceller(string name)
        {
            _registeredEchoCancellers.TryRemove(name, out _);
        }

        public void UnregisterPreprocessor(string name)
        {
            _registeredPreprocessors.TryRemove(name, out _);
        }

        public IEchoCanceller? CreateEchoCanceller(string name)
        {
            if (_registeredEchoCancellers.TryGetValue(name, out var echoCanceller))
            {
                return (IEchoCanceller?)Activator.CreateInstance(echoCanceller);
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

        public abstract string GetDefaultEchoCanceller();

        public abstract List<string> GetInputDevices();

        public abstract List<string> GetOutputDevices();

        public abstract List<string> GetPreprocessors();

        public abstract List<string> GetEchoCancellers();

        public abstract IAudioRecorder CreateAudioRecorder();

        public abstract IAudioPlayer CreateAudioPlayer();
    }
}
