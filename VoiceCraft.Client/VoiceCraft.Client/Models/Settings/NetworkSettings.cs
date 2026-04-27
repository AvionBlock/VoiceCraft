using System;
using VoiceCraft.Client.Services;
using VoiceCraft.Network;

namespace VoiceCraft.Client.Models.Settings;

public class NetworkSettings : Setting<NetworkSettings>
{
    public PositioningType PositioningType
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = PositioningType.Server;

    public string McWssListenIp
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = "127.0.0.1";

    public ushort McWssHostPort
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = 8080;

    public override event Action<NetworkSettings>? OnUpdated;

    public override object Clone()
    {
        var clone = (NetworkSettings)MemberwiseClone();
        clone.OnUpdated = null;
        return clone;
    }
}