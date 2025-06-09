using System;
using System.Collections.Generic;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings;

public class EntitySettings : Setting<EntitySettings>
{
    private Dictionary<Guid, EntitySetting> _entities = new();

    public Dictionary<Guid, EntitySetting> Entities
    {
        get => _entities;
        set
        {
            _entities = value;
            OnUpdated?.Invoke(this);
        }
    }

    public override event Action<EntitySettings>? OnUpdated;

    public override object Clone()
    {
        var clone = (EntitySettings)MemberwiseClone();
        clone.Entities = new Dictionary<Guid, EntitySetting>();
        foreach (var entity in _entities)
        {
            var clonedEntity = (EntitySetting)entity.Value.Clone();
            clone.Entities.TryAdd(entity.Key, clonedEntity);
        }
        clone.OnUpdated = null;
        return clone;
    }
}