using System;
using System.Net;
using LiteNetLib;
using VoiceCraft.Core;

namespace VoiceCraft.Network;

public abstract class VoiceCraftNetPeer(
    Version version,
    Guid userGuid,
    Guid serverUserGuid,
    string locale,
    PositioningType positioningType)
{
    public abstract VcConnectionState ConnectionState { get; }
    public abstract IPEndPoint EndPoint { get; }
    public Version Version { get; } = version;
    public Guid UserGuid { get; } = userGuid;
    public Guid ServerUserGuid { get; } = serverUserGuid;
    public string Locale { get; } = locale;
    public PositioningType PositioningType { get; } = positioningType;
    public object? Tag { get; set; }

    public abstract void Accept();
    public abstract void Reject();
    public abstract void Reject(Span<byte> data);
    public abstract void Send<T>(Span<byte> data, DeliveryMethod deliveryMethod);
    public abstract void Disconnect(Span<byte> data);
    public abstract void Disconnect();
}