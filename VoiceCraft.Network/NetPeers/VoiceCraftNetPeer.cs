using System;

namespace VoiceCraft.Network.NetPeers;

public abstract class VoiceCraftNetPeer(
    Version version,
    Guid userGuid,
    Guid serverUserGuid,
    string locale,
    PositioningType positioningType)
{
    public abstract VcConnectionState ConnectionState { get; }
    public Version Version { get; } = version;
    public Guid UserGuid { get; } = userGuid;
    public Guid ServerUserGuid { get; } = serverUserGuid;
    public string Locale { get; } = locale;
    public PositioningType PositioningType { get; } = positioningType;
    public object? Tag { get; set; }

    public abstract void Accept();
    public abstract void Reject(string? reason = null);
    public abstract void Send(ReadOnlySpan<byte> data, VcDeliveryMethod deliveryMethod);
    public abstract void Disconnect(string reason);
    public abstract void Disconnect();
}