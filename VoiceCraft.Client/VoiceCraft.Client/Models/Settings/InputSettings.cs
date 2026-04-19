using System;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings;

public class InputSettings : Setting<InputSettings>
{
    public string InputDevice
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = "Default";

    public float InputVolume
    {
        get;
        set
        {
            if (value is > 2 or < 0)
                throw new ArgumentException("Settings.Input.Validation.InputVolume");
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = 1.0f;

    public float MicrophoneSensitivity
    {
        get;
        set
        {
            if (value is > 1 or < 0)
                throw new ArgumentException("Settings.Input.Validation.MicrophonesSensitivity");
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = 0.04f;

    public Guid AutomaticGainController
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = Guid.Empty;

    public Guid Denoiser
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = Guid.Empty;

    public Guid EchoCanceler
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = Guid.Empty;

    public bool HardwarePreprocessorsEnabled
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = true;

    public bool PushToTalkEnabled
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    }

    public bool PushToTalkCue
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = true;

    public override event Action<InputSettings>? OnUpdated;

    public override object Clone()
    {
        var clone = (InputSettings)MemberwiseClone();
        clone.OnUpdated = null;
        return clone;
    }
}
