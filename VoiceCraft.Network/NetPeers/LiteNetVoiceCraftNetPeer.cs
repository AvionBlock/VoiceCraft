using System;
using LiteNetLib;
using VoiceCraft.Network.Servers;
using VoiceCraft.Network.World;

namespace VoiceCraft.Network.NetPeers;

public class LiteNetVoiceCraftNetPeer(
    VoiceCraftServer? server,
    NetPeer netPeer,
    Guid userGuid,
    Guid serverUserGuid,
    string locale,
    PositioningType positioningType) : VoiceCraftNetPeer(server, userGuid, serverUserGuid, locale, positioningType)
{
    public NetPeer NetPeer { get; } = netPeer;

    public override VcConnectionState ConnectionState
    {
        get
        {
            return NetPeer.ConnectionState switch
            {
                LiteNetLib.ConnectionState.Outgoing => VcConnectionState.Connecting,
                LiteNetLib.ConnectionState.Connected => VcConnectionState.Connected,
                LiteNetLib.ConnectionState.ShutdownRequested => VcConnectionState.Disconnecting,
                _ => VcConnectionState.Disconnected
            };
        }
    }
}