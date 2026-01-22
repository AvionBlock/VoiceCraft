using System;
using System.Net;

namespace VoiceCraft.Network.NetPeers;

public class HttpMcApiNetPeer(IPEndPoint endPoint) : McApiNetPeer
{
    private McApiConnectionState _connectionState;
    private string _sessionToken = string.Empty;
    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    public IPEndPoint EndPoint { get; } = endPoint;
    public override McApiConnectionState ConnectionState => _connectionState;
    public override string SessionToken => _sessionToken;

    public void SetConnectionState(McApiConnectionState state)
    {
        _connectionState = state;
    }
    
    public void SetSessionToken(string token)
    {
        _sessionToken = token;
    }
}