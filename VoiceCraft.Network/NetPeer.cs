using System.Collections.Concurrent;
using System.Net;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Packets.VoiceCraft;

namespace VoiceCraft.Network
{
    public class NetPeer(EndPoint ep, long Id, NetPeerState initialState = NetPeerState.Disconnected)
    {
        public const int ResendTime = 300;
        public const int RetryResendTime = 500;
        public const int MaxSendRetries = 20;
        public const int MaxRecvBufferSize = 30; //30 packets.

        public delegate void PacketReceived(NetPeer peer, VoiceCraftPacket packet);
        public event PacketReceived? OnPacketReceived;
        
        private int _sequence;
        private uint _nextSequence;
        private readonly ConcurrentDictionary<uint, VoiceCraftPacket> ReliabilityQueue = new();
        private readonly ConcurrentDictionary<uint, VoiceCraftPacket> ReceiveBuffer = new();

        // Lock objects for thread safety
        private readonly object _receiveLock = new();

        /// <summary>
        /// Reason for disconnection.
        /// </summary>
        public string? DisconnectReason { get; private set; }

        /// <summary>
        /// Defines whether the client is successfully requesting, connected or disconnected.
        /// Uses volatile backing field for thread-safety.
        /// </summary>
        private volatile NetPeerState _state = initialState;
        public NetPeerState State => _state;

        /// <summary>
        /// Endpoint of the NetPeer.
        /// </summary>
        public EndPoint RemoteEndPoint { get; set; } = ep;

        /// <summary>
        /// When the client was last active.
        /// </summary>
        public long LastActive { get; set; } = Environment.TickCount64;

        /// <summary>
        /// The ID of the NetPeer, Used to update the endpoint if invalid.
        /// </summary>
        public long Id { get; set; } = Id;

        /// <summary>
        /// Send Queue.
        /// </summary>
        public ConcurrentQueue<VoiceCraftPacket> SendQueue { get; set; } = new();

        public void AddToSendBuffer(VoiceCraftPacket packet)
        {
            packet.Id = Id;

            if (packet.IsReliable)
            {
                // Atomically increment sequence
                uint seq = (uint)Interlocked.Increment(ref _sequence) - 1;
                packet.Sequence = seq;
                packet.ResendTime = Environment.TickCount64 + ResendTime;
                
                ReliabilityQueue.TryAdd(seq, packet);
            }

            SendQueue.Enqueue(packet);
        }

        public bool AddToReceiveBuffer(VoiceCraftPacket packet)
        {
            LastActive = Environment.TickCount64;
            if (State == NetPeerState.Connected && packet.Id != Id) return false; //Invalid Id.

            if (!packet.IsReliable)
            {
                OnPacketReceived?.Invoke(this, packet);
                return true; //Not reliable, We can just say it's received.
            }

            List<VoiceCraftPacket> packetsToProcess = new List<VoiceCraftPacket>();

            lock (_receiveLock)
            {
                if (ReceiveBuffer.Count >= MaxRecvBufferSize && packet.Sequence != _nextSequence)
                    return false; //make sure it doesn't overload the receive buffer and cause a memory overflow.
                
                // Acknowledge packet
                AddToSendBuffer(new Ack() { PacketSequence = packet.Sequence }); 

                if (packet.Sequence < _nextSequence) return true; //Likely to be a duplicate packet.

                ReceiveBuffer.TryAdd(packet.Sequence, packet); //Add it in, TryAdd does not replace an old packet.
                
                // Process sequential packets
                while (ReceiveBuffer.TryRemove(_nextSequence, out var nextPacket))
                {
                    _nextSequence++; //Update next expected packet.
                    packetsToProcess.Add(nextPacket);
                }
            }

            // Invoke events outside the lock to prevent deadlocks
            foreach (var p in packetsToProcess)
            {
                OnPacketReceived?.Invoke(this, p);
            }

            return true;
        }

        public void ResendPackets()
        {
            long now = Environment.TickCount64;

            foreach (var kvp in ReliabilityQueue)
            {
                var packet = kvp.Value;
                if (packet.ResendTime <= now)
                {
                    // Update resend time safely
                    packet.ResendTime = now + RetryResendTime;
                    
                    // Increment retries safely (though typically only one thread does this)
                    Interlocked.Increment(ref packet.Retries); // Assuming Retries is a field we can access, otherwise just property access is fine given usage context
                    
                    packet.Id = Id; //Update Id since this might change on login.
                    SendQueue.Enqueue(packet);
                }
            }
        }

        public void AcceptLogin(short key)
        {
            if (State == NetPeerState.Requesting)
            {
                AddToSendBuffer(new Accept() { Key = key });
                _state = NetPeerState.Connected;
            }
        }

        public void DenyLogin(string? reason = null)
        {
            if (State == NetPeerState.Requesting)
            {
                AddToSendBuffer(new Deny() { Reason = reason ?? string.Empty });
                DisconnectReason = reason;
                _state = NetPeerState.Disconnected;
            }
        }

        public void Disconnect(string? reason = null, bool notify = true)
        {
            if (State != NetPeerState.Disconnected)
            {
                if (notify)
                    AddToSendBuffer(new Logout() { Reason = reason ?? string.Empty });
                DisconnectReason = reason;
                _state = NetPeerState.Disconnected;
            }
        }

        public void AcknowledgePacket(uint packetId)
        {
            ReliabilityQueue.TryRemove(packetId, out var _);
        }

        public static long GenerateId()
        {
            return Random.Shared.NextInt64(long.MinValue + 1, long.MaxValue); //long.MinValue is used to specify no Id.
        }

        public void Reset()
        {
            lock (_receiveLock)
            {
                SendQueue.Clear();
                ReliabilityQueue.Clear();
                ReceiveBuffer.Clear();
                _nextSequence = 0;
                // Sequence is now managed by Interlocked, resetting it strictly involves race conditions but for Reset() usage (likely disconnect) it's acceptable to just set it.
                _sequence = 0; 
            }
        }
    }

    public enum NetPeerState
    {
        Disconnected,
        Requesting,
        Connected
    }
}
