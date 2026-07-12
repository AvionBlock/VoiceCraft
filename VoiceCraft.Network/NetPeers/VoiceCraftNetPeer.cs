using System;
using VoiceCraft.Network.Servers;

namespace VoiceCraft.Network.NetPeers;

public abstract class VoiceCraftNetPeer(
    VoiceCraftServer? server,
    Guid userGuid,
    Guid serverUserGuid,
    string locale,
    PositioningType positioningType)
{
    public abstract VcConnectionState ConnectionState { get; }
    public VoiceCraftServer? Server { get; } = server;
    public Guid UserGuid { get; } = userGuid;
    public Guid ServerUserGuid { get; } = serverUserGuid;
    public string Locale { get; } = locale;
    public PositioningType PositioningType { get; } = positioningType;
    public object? Tag { get; set; }
}