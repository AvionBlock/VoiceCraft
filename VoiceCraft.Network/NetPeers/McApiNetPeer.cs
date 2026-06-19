using System.Collections.Concurrent;
using System.Collections.Generic;
using VoiceCraft.Network.Servers;

namespace VoiceCraft.Network.NetPeers;

public abstract class McApiNetPeer(McApiServer? server)
{
    public ConcurrentQueue<QueuedPacket> IncomingQueue { get; } = new();
    public ConcurrentQueue<QueuedPacket> OutgoingQueue { get; } = new();
    public HashSet<EventType> SubscribedEvents { get; } = [];
    
    public virtual McApiConnectionState ConnectionState { get; set; }
    public McApiServer? Server { get; } = server;
    public abstract string SessionToken { get; }
    public object? Tag { get; set; }
    
    public struct QueuedPacket(byte[] data, string token)
    {
        public readonly byte[] Data = data;
        public readonly string Token = token;
    }
}