using System;

namespace VoiceCraft.Network.NetPeers;

public class HttpMcApiNetPeer : McApiNetPeer
{
    private McApiConnectionState _connectionState;
    private string _sessionToken = string.Empty;
    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    public string LookupToken { get; private set; } = string.Empty;

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

    public void SetLookupToken(string token)
    {
        LookupToken = token;
    }
}
