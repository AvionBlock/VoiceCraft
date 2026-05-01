using System;
using Fleck;

namespace VoiceCraft.Network.NetPeers;

public class McWssMcApiNetPeer(IWebSocketConnection connection) : McApiNetPeer
{
    private string _sessionToken = string.Empty;
    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    public IWebSocketConnection Connection { get; } = connection;
    public override string SessionToken => _sessionToken;
    
    public void SetSessionToken(string token)
    {
        _sessionToken = token;
    }
}