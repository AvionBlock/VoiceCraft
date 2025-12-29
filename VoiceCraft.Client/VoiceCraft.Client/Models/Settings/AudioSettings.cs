using System;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings;

public class AudioSettings : Setting<AudioSettings>
{
    private Guid _automaticGainController = Guid.Empty;
    private Guid _denoiser = Guid.Empty;
    private Guid _echoCanceler = Guid.Empty;

    private string _inputDevice = "Default";
    private string _outputDevice = "Default";
    private float _microphoneSensitivity = 0.04f;
    private float _outputVolume = 1.0f;

    public Guid AutomaticGainController
    {
        get => _automaticGainController;
        set
        {
            _automaticGainController = value;
            OnUpdated?.Invoke(this);
        }
    }

    public Guid Denoiser
    {
        get => _denoiser;
        set
        {
            _denoiser = value;
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


    public float MicrophoneSensitivity
    {
        get => _microphoneSensitivity;
        set
        {
            if (value is > 1 or < 0)
                throw new ArgumentException("Microphone sensitivity must be between 0 and 1.");
            _microphoneSensitivity = value;
            OnUpdated?.Invoke(this);
        }
    }

    public float OutputVolume
    {
        get => _outputVolume;
        set
        {
            if (value is > 2 or < 0)
                throw new ArgumentException("Output volume must be between 0 and 2.");
            _outputVolume = value;
            OnUpdated?.Invoke(this);
        }
    }

    public override event Action<AudioSettings>? OnUpdated;

    public override object Clone()
    {
        var clone = (AudioSettings)MemberwiseClone();
        clone.OnUpdated = null;
        return clone;
    }
}