using System;
using SIPSorcery.Net;

namespace VoiceCraft.Network.NetPeers;

public class WebRtcVoiceCraftNetPeer(
    RTCDataChannel dataChannel,
    Guid userGuid,
    Guid serverUserGuid,
    string locale,
    PositioningType positioningType)
    : VoiceCraftNetPeer(userGuid, serverUserGuid, locale, positioningType)
{
    private VcConnectionState _connectionState = VcConnectionState.Connected;

    public RTCDataChannel DataChannel { get; } = dataChannel;
    public override VcConnectionState ConnectionState => _connectionState;

    public void SetConnectionState(VcConnectionState connectionState)
    {
        _connectionState = connectionState;
    }
}
