using System;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings;

public class NotificationSettings : Setting<NotificationSettings>
{
    private bool _disableNotifications;

    private ushort _dismissDelayMs = 2000;

    public ushort DismissDelayMs
    {
        get => _dismissDelayMs;
        set
        {
            _dismissDelayMs = value;
            OnUpdated?.Invoke(this);
        }
    }

    public bool DisableNotifications
    {
        get => _disableNotifications;
        set
        {
            _disableNotifications = value;
            OnUpdated?.Invoke(this);
        }
    }

    public override event Action<NotificationSettings>? OnUpdated;

    public override object Clone()
    {
        var clone = (NotificationSettings)MemberwiseClone();
        clone.OnUpdated = null;
        return clone;
    }
}