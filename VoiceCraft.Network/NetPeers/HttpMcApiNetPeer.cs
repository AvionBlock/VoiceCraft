using System.Net;

namespace VoiceCraft.Network.NetPeers;

public class HttpMcApiNetPeer(IPEndPoint endPoint) : McApiNetPeer
{
    public IPEndPoint EndPoint { get; }
    public override McApiConnectionState ConnectionState { get; }
}