using System;
using VoiceCraft.Client.Services;
using VoiceCraft.Network;

namespace VoiceCraft.Client.Models.Settings;

public class NetworkSettings : Setting<NetworkSettings>
{
    private ushort _mcWssHostPort = 8080;
    private string _mcWssListenIp = "127.0.0.1";
    private PositioningType _positioningType = PositioningType.Server;

    public PositioningType PositioningType
    {
        get => _positioningType;
        set
        {
            _positioningType = value;
            OnUpdated?.Invoke(this);
        }
    }

    public string McWssListenIp
    {
        get => _mcWssListenIp;
        set
        {
            _mcWssListenIp = value;
            OnUpdated?.Invoke(this);
        }
    }

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