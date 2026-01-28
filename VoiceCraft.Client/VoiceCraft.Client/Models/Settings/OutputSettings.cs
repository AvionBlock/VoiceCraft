using System;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Models.Settings;

public class OutputSettings : Setting<OutputSettings>
{
    private string _outputDevice = "Default";
    private float _outputVolume = 1.0f;
    private Guid _audioClipper = Constants.HardAudioClipperGuid; //Set as default on initialize.

    public string OutputDevice
    {
        get => _outputDevice;
        set
        {
            _outputDevice = value;
            OnUpdated?.Invoke(this);
        }
    }

    public float OutputVolume
    {
        get => _outputVolume;
        set
        {
            if (value is > 2 or < 0)
                throw new ArgumentException("Output Volume must be between 0 and 2.");
            _outputVolume = value;
            OnUpdated?.Invoke(this);
        }
    }

    public Guid AudioClipper
    {
        get => _audioClipper;
        set
        {
            _audioClipper = value;
            OnUpdated?.Invoke(this);
        }
    }

    public override event Action<OutputSettings>? OnUpdated;

    public override object Clone()
    {
        var clone = (OutputSettings)MemberwiseClone();
        clone.OnUpdated = null;
        return clone;
    }
}