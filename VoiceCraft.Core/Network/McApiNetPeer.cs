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

        public DateTime LastPing { get; set; } = DateTime.UtcNow;
        public bool Connected { get; private set; }
        public string Token { get; private set; } = string.Empty;
        public int OutgoingQueueCount { get; set; }

        public event Action<McApiNetPeer>? OnConnected;
        public event Action<McApiNetPeer>? OnDisconnected;

        ~McApiNetPeer()
        {
            OnConnected = null;
            OnDisconnected = null;
        }

        public void Disconnect()
        {
            if (!Connected) return;
            Connected = false;
            Token = string.Empty;
            OnDisconnected?.Invoke(this);
        }

        public void AcceptConnection(string sessionToken)
        {
            if (Connected) return;
            Token = sessionToken;
            Connected = true;
            LastPing = DateTime.UtcNow;
            OnConnected?.Invoke(this);
        }

        public void ReceiveInboundPacket(byte[] packet)
        {
            if (packet.Length > short.MaxValue)
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
            if (writer.Length > short.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(writer));

            _outboundPacketQueue.Enqueue(writer.CopyData());
        }

        public bool RetrieveOutboundPacket([NotNullWhen(true)] out byte[]? packet)
        {
            return _outboundPacketQueue.TryDequeue(out packet);
        }
    }
}