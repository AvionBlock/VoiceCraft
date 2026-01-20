using System;
using LiteNetLib;

namespace VoiceCraft.Network.NetPeers;

public class LiteNetVoiceCraftNetPeer(
    NetPeer netPeer,
    Guid userGuid,
    Guid serverUserGuid,
    string locale,
    PositioningType positioningType) : VoiceCraftNetPeer(userGuid, serverUserGuid, locale, positioningType)
{
    public NetPeer NetPeer => netPeer;
    
    public override VcConnectionState ConnectionState
    {
        get
        {
            return netPeer.ConnectionState switch
            {
                LiteNetLib.ConnectionState.Outgoing => VcConnectionState.Connecting,
                LiteNetLib.ConnectionState.Connected => VcConnectionState.Connected,
                LiteNetLib.ConnectionState.ShutdownRequested => VcConnectionState.Disconnecting,
                _ => VcConnectionState.Disconnected
            };
        }
    }
}