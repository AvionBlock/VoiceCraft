using System;
using System.Collections.Generic;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings;

public class UserSettings : Setting<UserSettings>
{
    private Dictionary<Guid, UserSetting> _users = new();

    public Dictionary<Guid, UserSetting> Users
    {
        get => _users;
        set
        {
            _users = value;
            OnUpdated?.Invoke(this);
        }
    }

    public override event Action<UserSettings>? OnUpdated;

    public override object Clone()
    {
        var clone = (UserSettings)MemberwiseClone();
        clone.Users = new Dictionary<Guid, UserSetting>();
        foreach (var user in _users)
        {
            var clonedEntity = (UserSetting)user.Value.Clone();
            clone.Users.TryAdd(user.Key, clonedEntity);
        }

        clone.OnUpdated = null;
        return clone;
    }
}