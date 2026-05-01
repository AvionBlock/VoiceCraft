using System;

namespace VoiceCraft.Network.NetPeers;

public class HttpMcApiNetPeer : McApiNetPeer
{
    private string _sessionToken = string.Empty;
    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    
    public override string SessionToken => _sessionToken;
    
    public void SetSessionToken(string token)
    {
        _sessionToken = token;
    }
}
