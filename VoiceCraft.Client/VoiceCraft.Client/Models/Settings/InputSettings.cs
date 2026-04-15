using System;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings;

public class InputSettings : Setting<InputSettings>
{
    private string _inputDevice = "Default";
    private float _inputVolume = 1.0f;
    private float _microphoneSensitivity = 0.04f;
    
    private Guid _automaticGainController = Guid.Empty;
    private Guid _denoiser = Guid.Empty;
    private Guid _echoCanceler = Guid.Empty;
    
    private bool _pushToTalkEnabled;
    private bool _pushToTalkCue = true;

    public string InputDevice
    {
        get => _inputDevice;
        set
        {
            _inputDevice = value;
            OnUpdated?.Invoke(this);
        }
    }

    public float InputVolume
    {
        get => _inputVolume;
        set
        {
            if(value is > 2 or < 0)
                throw new ArgumentException("Settings.Input.Validation.InputVolume");
            _inputVolume = value;
            OnUpdated?.Invoke(this);
        }
    }

    public float MicrophoneSensitivity
    {
        get => _microphoneSensitivity;
        set
        {
            if (value is > 1 or < 0)
                throw new ArgumentException("Settings.Input.Validation.MicrophonesSensitivity");
            _microphoneSensitivity = value;
            OnUpdated?.Invoke(this);
        }
    }
    
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

    public bool PushToTalkEnabled
    {
        get => _pushToTalkEnabled;
        set
        {
            _pushToTalkEnabled = value;
            OnUpdated?.Invoke(this);
        }
    }

    public bool PushToTalkCue
    {
        get => _pushToTalkCue;
        set
        {
            _pushToTalkCue = value;
            OnUpdated?.Invoke(this);
        }
    }

    public override event Action<InputSettings>? OnUpdated;

    public override object Clone()
    {
        var clone = (InputSettings)MemberwiseClone();
        clone.OnUpdated = null;
        return clone;
    }
}