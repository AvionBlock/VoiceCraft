using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network
{
    public class McApiNetPeer
    {
        private readonly ConcurrentQueue<byte[]> _inboundPacketQueue = new ConcurrentQueue<byte[]>();
        private readonly ConcurrentQueue<byte[]> _outboundPacketQueue = new ConcurrentQueue<byte[]>();
        
        public DateTime LastPing = DateTime.UtcNow;
        public object Metadata = new object();
        public bool Connected { get; set; }
        public string SessionToken { get; }

        public McApiNetPeer(string sessionToken)
        {
            SessionToken = sessionToken;
        }

        public void ReceiveInboundPacket(byte[] packet)
        {
            if (!Connected) return;
            
            _inboundPacketQueue.Enqueue(packet);
        }
        
        public bool RetrieveInboundPacket([NotNullWhen(true)] out byte[]? packet)
        {
            return _inboundPacketQueue.TryDequeue(out packet);
        }

        public void SendPacket(NetDataWriter writer)
        {
            if (!Connected) return;
            
            _outboundPacketQueue.Enqueue(writer.CopyData()); //Will need to fragment it here.
        }

        public bool RetrieveOutboundPacket([NotNullWhen(true)] out byte[]? packet)
        {
            return _outboundPacketQueue.TryDequeue(out packet);
        }
    }
}