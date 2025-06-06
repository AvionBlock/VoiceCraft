using System;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings;

public class NetworkSettings : Setting<NetworkSettings>
{
    private ushort _mcWssHostPort = 8080;

    public ushort McWssHostPort
    {
        get => _mcWssHostPort;
        set
        {
            _mcWssHostPort = value;
            OnUpdated?.Invoke(this);
        }
    }
    
    public override event Action<NetworkSettings>? OnUpdated;

    public override object Clone()
    {
        var clone = (NetworkSettings)MemberwiseClone();
        clone.OnUpdated = null;
        return clone;
    }
}