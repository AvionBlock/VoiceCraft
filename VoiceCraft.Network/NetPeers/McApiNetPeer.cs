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
    
    public struct QueuedPacket
    {
        public readonly byte[] ByteData;
        public readonly string StringData;
        public readonly string Token;

        public QueuedPacket(object data, string token)
        {
            Token = token;
            switch (data)
            {
                case byte[] byteData:
                    StringData = string.Empty;
                    ByteData = byteData;
                    break;
                case string stringData:
                    ByteData = [];
                    StringData = stringData;
                    break;
                default:
                    throw new InvalidOperationException("data must be a string or byte array!");
            }
        }
    }
}