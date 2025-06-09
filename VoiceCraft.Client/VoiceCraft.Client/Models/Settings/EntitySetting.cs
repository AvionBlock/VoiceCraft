using System;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings;

public class EntitySetting : Setting<EntitySetting>
{
    private float _volume = 1f;
    private bool _muted;

    public float Volume
    {
        get => _volume;
        set
        {
            _volume = value;
            OnUpdated?.Invoke(this);
        }
    }

    public bool Muted
    {
        get => _muted;
        set
        {
            _muted = value;
            OnUpdated?.Invoke(this);
        }
    }

    public override event Action<EntitySetting>? OnUpdated;
    
    public override object Clone()
    {
        var clone = (EntitySetting)MemberwiseClone();
        clone.OnUpdated = null;
        return clone;
    }
}