using System;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings;

public class LocaleSettings : Setting<LocaleSettings>
{
    private string _culture = "en-US";

    public string Culture
    {
        get => _culture;
        set
        {
            _culture = value;
            OnUpdated?.Invoke(this);
        }
    }

    public override event Action<LocaleSettings>? OnUpdated;

    public override object Clone()
    {
        var clone = (LocaleSettings)MemberwiseClone();
        clone.OnUpdated = null;
        return clone;
    }
}