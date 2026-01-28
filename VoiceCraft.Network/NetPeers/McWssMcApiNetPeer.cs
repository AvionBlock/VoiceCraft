using System;
using Fleck;

namespace VoiceCraft.Network.NetPeers;

public class McWssMcApiNetPeer(IWebSocketConnection connection) : McApiNetPeer
{
    private McApiConnectionState _connectionState;
    private string _sessionToken = string.Empty;
    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    public IWebSocketConnection Connection { get; } = connection;
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