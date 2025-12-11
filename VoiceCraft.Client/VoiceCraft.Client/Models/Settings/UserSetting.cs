using System;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings;

public class UserSetting : Setting<UserSetting>
{
    private bool _userMuted;
    private float _volume = 1f;

    public float Volume
    {
        get => _volume;
        set
        {
            _volume = value;
            OnUpdated?.Invoke(this);
        }
    }

    public bool UserMuted
    {
        get => _userMuted;
        set
        {
            _userMuted = value;
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