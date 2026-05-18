using System;
using System.Collections.Generic;
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

    public List<WebRtcIceServerSettings> WebRtcIceServers
    {
        get;
        set
        {
            field = value ?? [];
            OnUpdated?.Invoke(this);
        }
    } =
    [
        new() { Urls = "stun:stun.l.google.com:19302" }
    ];

    public override event Action<NetworkSettings>? OnUpdated;

    public override object Clone()
    {
        var clone = (NetworkSettings)MemberwiseClone();
        clone.OnUpdated = null;
        return clone;
    }
}

public class WebRtcIceServerSettings
{
    public string Urls { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Credential { get; set; }
}
