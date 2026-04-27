using System;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings;

public class TelemetrySettings : Setting<TelemetrySettings>
{
    public bool Enabled
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = true;

    public bool ConsentShown
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    }

    public override event Action<TelemetrySettings>? OnUpdated;

    public override object Clone()
    {
        var clone = (TelemetrySettings)MemberwiseClone();
        clone.OnUpdated = null;
        return clone;
    }
}
