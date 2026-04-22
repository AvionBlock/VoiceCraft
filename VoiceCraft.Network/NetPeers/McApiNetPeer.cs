using System;
using System.Collections.Concurrent;

namespace VoiceCraft.Network.NetPeers;

public abstract class McApiNetPeer
{
    public ConcurrentQueue<QueuedPacket> IncomingQueue { get; } = new();
    public ConcurrentQueue<QueuedPacket> OutgoingQueue { get; } = new();
    
    public abstract McApiConnectionState ConnectionState { get; }
    public abstract string SessionToken { get; }
    public object? Tag { get; set; }
    
    public struct QueuedPacket(byte[] data, string token)
    {
        public readonly byte[] Data = data;
        public readonly string Token = token;
    }
}