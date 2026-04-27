using System;
using System.Collections.Generic;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings;

public class HotKeySettings : Setting<HotKeySettings>
{
    public Dictionary<string, string> Bindings
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = new();

    public override event Action<HotKeySettings>? OnUpdated;

    public override object Clone()
    {
        var clone = (HotKeySettings)MemberwiseClone();
        clone.Bindings = new Dictionary<string, string>(Bindings, StringComparer.Ordinal);
        clone.OnUpdated = null;
        return clone;
    }
}
