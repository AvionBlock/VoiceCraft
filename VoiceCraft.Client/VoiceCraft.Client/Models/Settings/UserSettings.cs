using System;
using System.Collections.Generic;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings;

public class UserSettings : Setting<UserSettings>
{
    public Dictionary<Guid, UserSetting> Users
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = new();

    public override event Action<UserSettings>? OnUpdated;

    public override object Clone()
    {
        var clone = (UserSettings)MemberwiseClone();
        clone.Users = new Dictionary<Guid, UserSetting>();
        foreach (var user in Users)
        {
            var clonedEntity = (UserSetting)user.Value.Clone();
            clone.Users.TryAdd(user.Key, clonedEntity);
        }

        clone.OnUpdated = null;
        return clone;
    }
}