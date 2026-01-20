using System;

namespace VoiceCraft.Network.NetPeers;

public abstract class VoiceCraftNetPeer(
    Guid userGuid,
    Guid serverUserGuid,
    string locale,
    PositioningType positioningType)
{
    public abstract VcConnectionState ConnectionState { get; }
    public Guid UserGuid { get; } = userGuid;
    public Guid ServerUserGuid { get; } = serverUserGuid;
    public string Locale { get; } = locale;
    public PositioningType PositioningType { get; } = positioningType;
    public object? Tag { get; set; }
}