using System;
using System.Collections.Generic;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings;

public class HotKeySettings : Setting<HotKeySettings>
{
    private Dictionary<string, string> _bindings = new();
    private bool _playPushToTalkCues = true;

    public Dictionary<string, string> Bindings
    {
        get => _bindings;
        set
        {
            _bindings = value;
            OnUpdated?.Invoke(this);
        }
    }

    public bool PlayPushToTalkCues
    {
        get => _playPushToTalkCues;
        set
        {
            _playPushToTalkCues = value;
            OnUpdated?.Invoke(this);
        }
    }

    public override event Action<HotKeySettings>? OnUpdated;

    public override object Clone()
    {
        var clone = (HotKeySettings)MemberwiseClone();
        clone.Bindings = new Dictionary<string, string>(_bindings, StringComparer.Ordinal);
        clone.OnUpdated = null;
        return clone;
    }
}
