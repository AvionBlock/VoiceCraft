using System;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings
{
    public class AudioSettings : Setting<AudioSettings>
    {
        public override event Action<AudioSettings>? OnUpdated;

        public string InputDevice
        {
            get => _inputDevice;
            set
            {
                _inputDevice = value;
                OnUpdated?.Invoke(this);
            }
        }

        public string OutputDevice
        {
            get => _outputDevice;
            set
            {
                _outputDevice = value;
                OnUpdated?.Invoke(this);
            }
        }

        public Guid Preprocessor
        {
            get => _preprocessor;
            set
            {
                _preprocessor = value;
                OnUpdated?.Invoke(this);
            }
        }

        public Guid EchoCanceler
        {
            get => _echoCanceler;
            set
            {
                _echoCanceler = value;
                OnUpdated?.Invoke(this);
            }
        }

        public float MicrophoneSensitivity
        {
            get => _microphoneSensitivity;
            set
            {
                if(value > 1 || value < 0)
                    throw new ArgumentException("Microphone sensitivity must be between 0 and 1.");
                _microphoneSensitivity = value;
                OnUpdated?.Invoke(this);
            }
        }

        public bool AEC
        {
            get => _aec;
            set
            {
                _aec = value;
                OnUpdated?.Invoke(this);
            }
        }

        public bool AGC
        {
            get => _agc;
            set
            {
                _agc = value;
                OnUpdated?.Invoke(this);
            }
        }

        public bool Denoiser
        {
            get => _denoiser;
            set
            {
                _denoiser = value;
                OnUpdated?.Invoke(this);
            }
        }

        public bool VAD
        {
            get => _vad;
            set
            {
                _vad = value;
                OnUpdated?.Invoke(this);
            }
        }

        public string _inputDevice = "Default";
        public string _outputDevice = "Default";
        public Guid _preprocessor = Guid.Empty;
        public Guid _echoCanceler = Guid.Empty;
        public float _microphoneSensitivity = 0.04f;
        public bool _aec;
        public bool _agc;
        public bool _denoiser;
        public bool _vad;

        public override object Clone()
        {
            return MemberwiseClone();
        }
    }
}