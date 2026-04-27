using System;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Models.Settings;

public class OutputSettings : Setting<OutputSettings>
{
    public string OutputDevice
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = "Default";

    public float OutputVolume
    {
        get;
        set
        {
            if (value is > 2 or < 0)
                throw new ArgumentException("Settings.Input.Validation.OutputVolume");
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = 1.0f;

    public Guid AudioClipper
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = Constants.TanhSoftAudioClipperGuid;

    public override event Action<OutputSettings>? OnUpdated;

    public override object Clone()
    {
        var clone = (OutputSettings)MemberwiseClone();
        clone.OnUpdated = null;
        return clone;
    }
}