using System;
using VoiceCraft.Network.Servers;

namespace VoiceCraft.Network.NetPeers;

public class HttpMcApiNetPeer(HttpMcApiServer? httpMcApiServer) : McApiNetPeer(httpMcApiServer)
{
    private string _sessionToken = string.Empty;
    private string _lookupToken = string.Empty;
    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    public override string SessionToken => _sessionToken;
    public string LookupToken => string.IsNullOrWhiteSpace(_sessionToken) ? _lookupToken : _sessionToken;

    public void SetSessionToken(string token)
    {
        _sessionToken = token;
    }

    public void SetLookupToken(string token)
    {
        _lookupToken = token;
    }
}