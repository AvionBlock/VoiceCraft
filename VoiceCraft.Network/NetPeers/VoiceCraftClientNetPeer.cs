using System;
using VoiceCraft.Core;

namespace VoiceCraft.Network.NetPeers;

public class VoiceCraftClientNetPeer(
    Version version,
    Guid userGuid,
    Guid serverUserGuid,
    string locale,
    PositioningType positioningType) : VoiceCraftNetPeer(version, userGuid, serverUserGuid, locale, positioningType)
{
    public override VcConnectionState ConnectionState => VcConnectionState.Disconnected;

    public override void Accept()
    {
        throw new NotSupportedException();
    }

    public override void Reject()
    {
        throw new NotSupportedException();
    }

    public override void Reject(Span<byte> data)
    {
        throw new NotSupportedException();
    }

    public override void Send<T>(Span<byte> data, VcDeliveryMethod deliveryMethod)
    {
        throw new NotSupportedException();
    }

    public override void Disconnect(Span<byte> data)
    {
        throw new NotSupportedException();
    }

    public override void Disconnect()
    {
        throw new NotSupportedException();
    }
}