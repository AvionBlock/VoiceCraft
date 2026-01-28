using System.Collections.Concurrent;

namespace VoiceCraft.Network.NetPeers;

public abstract class McApiNetPeer
{
    public ConcurrentQueue<QueuedPacket> IncomingQueue { get; } = new();
    public ConcurrentQueue<QueuedPacket> OutgoingQueue { get; } = new();
    
    public abstract McApiConnectionState ConnectionState { get; }
    public abstract string SessionToken { get; }
    public object? Tag { get; set; }
    
    public struct QueuedPacket(string data, string token)
    {
        public string Data = data;
        public string Token = token;
    }
}