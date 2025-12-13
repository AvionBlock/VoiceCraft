using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network
{
    public class McApiNetPeer
    {
        private readonly ConcurrentQueue<QueuedPacket> _inboundPacketQueue = new ConcurrentQueue<QueuedPacket>();
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

        public void AcceptConnection(string token)
        {
            if (Connected) return;
            Token = token;
            Connected = true;
            LastPing = DateTime.UtcNow;
            OnConnected?.Invoke(this);
        }

        public void ReceiveInboundPacket(byte[] packet, string? token = null)
        {
            if (packet.Length > short.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(packet));

            LastPing = DateTime.UtcNow;
            _inboundPacketQueue.Enqueue(new QueuedPacket() { Data = packet, Token =  token });
        }

        public bool RetrieveInboundPacket([NotNullWhen(true)] out byte[]? packet, out string? token)
        {
            if (_inboundPacketQueue.TryDequeue(out var queuedPacket))
            {
                packet = queuedPacket.Data;
                token = queuedPacket.Token;
                return true;
            }

            packet = null;
            token = null;
            return false;
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

        private struct QueuedPacket
        {
            public byte[] Data;
            public string? Token;
        }
    }
}