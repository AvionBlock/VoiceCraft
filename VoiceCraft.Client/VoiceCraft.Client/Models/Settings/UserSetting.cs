using System;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings;

public class UserSetting : Setting<UserSetting>
{
    public float Volume
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = 1f;

    public bool UserMuted
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    }

    public override event Action<UserSetting>? OnUpdated;

    public override object Clone()
    {
        var clone = (UserSetting)MemberwiseClone();
        clone.OnUpdated = null;
        return clone;
    }
}