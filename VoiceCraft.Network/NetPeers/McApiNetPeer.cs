using System;

namespace VoiceCraft.Network.NetPeers;

public abstract class McApiNetPeer
{
    public abstract McApiConnectionState ConnectionState { get; }
    public string SessionToken { get; } = Guid.NewGuid().ToString();
    public object? Tag { get; set; }
}