using System;
using System.Collections.Concurrent;

namespace VoiceCraft.Core.Network
{
    public class McApiNetPeer
    {
        public DateTime LastPing = DateTime.MinValue;
        public object Metadata = new object();
        
        public ConcurrentQueue<byte[]> PacketQueue = new ConcurrentQueue<byte[]>();
    }
}