using System;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings;

public class NotificationSettings : Setting<NotificationSettings>
{
    public bool DisableNotifications
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    }

    public ushort DismissDelayMs
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = 2000;

    public override event Action<NotificationSettings>? OnUpdated;

    public override object Clone()
    {
        var clone = (NotificationSettings)MemberwiseClone();
        clone.OnUpdated = null;
        return clone;
    }
}