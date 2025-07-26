using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network
{
    public class McApiNetPeer
    {
        private readonly ConcurrentQueue<byte[]> _inboundPacketQueue = new ConcurrentQueue<byte[]>();
        private readonly ConcurrentQueue<byte[]> _outboundPacketQueue = new ConcurrentQueue<byte[]>();

        public event Action? OnDisconnected;
        
        public DateTime LastPing { get; set; } = DateTime.UtcNow;
        public bool Connected { get; private set; }
        public string SessionToken { get; private set; } = string.Empty;

        public void Disconnect()
        {
            if (!Connected) return;
            Connected = false;
            SessionToken = string.Empty;
            OnDisconnected?.Invoke();
            
            Debug.WriteLine("McApi Client Disconnected");
        }

        public void AcceptConnection(string sessionToken)
        {
            if (Connected) return;
            SessionToken = sessionToken;
            Connected = true;
            LastPing = DateTime.UtcNow;
            Debug.WriteLine($"McApi Client Connected: Session Token - {sessionToken}");
        }

        public void ReceiveInboundPacket(byte[] packet)
        {
            if (packet.Length > Constants.McApiMtuLimit)
                throw new ArgumentOutOfRangeException(nameof(packet));
            
            LastPing = DateTime.UtcNow;
            _inboundPacketQueue.Enqueue(packet);
        }
        
        public bool RetrieveInboundPacket([NotNullWhen(true)] out byte[]? packet)
        {
            return _inboundPacketQueue.TryDequeue(out packet);
        }

        public void SendPacket(NetDataWriter writer)
        {
            if (!Connected) return;
            if (writer.Length > Constants.McApiMtuLimit)
                throw new ArgumentOutOfRangeException(nameof(writer));
            
            _outboundPacketQueue.Enqueue(writer.CopyData());
        }

        public bool RetrieveOutboundPacket([NotNullWhen(true)] out byte[]? packet)
        {
            return _outboundPacketQueue.TryDequeue(out packet);
        }
    }
}