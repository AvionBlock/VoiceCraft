using System;
using Fleck;
using VoiceCraft.Network.Servers;

namespace VoiceCraft.Network.NetPeers;

public class McWssMcApiNetPeer(McWssMcApiServer? server, IWebSocketConnection connection) : McApiNetPeer(server)
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