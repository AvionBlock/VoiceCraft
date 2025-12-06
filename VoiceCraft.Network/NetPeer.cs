using System.Collections.Concurrent;
using System.Net;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Packets.VoiceCraft;

namespace VoiceCraft.Network;

/// <summary>
/// Represents a network peer with reliable and unreliable packet transmission support.
/// Handles packet sequencing, acknowledgment, and retry logic.
/// </summary>
/// <param name="ep">The remote endpoint of the peer.</param>
/// <param name="Id">The unique identifier for this peer.</param>
/// <param name="initialState">The initial connection state.</param>
public class NetPeer(EndPoint ep, long Id, NetPeerState initialState = NetPeerState.Disconnected)
{
    /// <summary>
    /// Initial resend time in milliseconds for reliable packets.
    /// </summary>
    public const int ResendTime = 300;
    
    /// <summary>
    /// Retry resend time in milliseconds after initial attempt.
    /// </summary>
    public const int RetryResendTime = 500;
    
    /// <summary>
    /// Maximum number of retry attempts for reliable packets.
    /// </summary>
    public const int MaxSendRetries = 20;
    
    /// <summary>
    /// Maximum size of the receive buffer in packets.
    /// </summary>
    public const int MaxRecvBufferSize = 30;

    /// <summary>
    /// Delegate for packet received events.
    /// </summary>
    public delegate void PacketReceived(NetPeer peer, VoiceCraftPacket packet);
    
    /// <summary>
    /// Event raised when a packet is received and ready to be processed.
    /// </summary>
    public event PacketReceived? OnPacketReceived;
    
    private int _sequence;
    private uint _nextSequence;
    private readonly ConcurrentDictionary<uint, VoiceCraftPacket> _reliabilityQueue = new();
    private readonly ConcurrentDictionary<uint, VoiceCraftPacket> _receiveBuffer = new();

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
    
    /// <summary>
    /// Gets the current connection state of this peer.
    /// </summary>
    public NetPeerState State => _state;

    /// <summary>
    /// Gets or sets the remote endpoint of this peer.
    /// </summary>
    public EndPoint RemoteEndPoint { get; set; } = ep;

    /// <summary>
    /// Gets or sets the tick count when this peer was last active.
    /// </summary>
    public long LastActive { get; set; } = Environment.TickCount64;

    /// <summary>
    /// Gets or sets the unique identifier for this peer.
    /// Used to update the endpoint if invalid.
    /// </summary>
    public long Id { get; set; } = Id;

    /// <summary>
    /// Gets the queue of packets waiting to be sent.
    /// </summary>
    public ConcurrentQueue<VoiceCraftPacket> SendQueue { get; } = new();

    /// <summary>
    /// Adds a packet to the send buffer, handling reliability if needed.
    /// </summary>
    /// <param name="packet">The packet to send.</param>
    public void AddToSendBuffer(VoiceCraftPacket packet)
    {
        packet.Id = Id;

        if (packet.IsReliable)
        {
            // Atomically increment sequence
            uint seq = (uint)Interlocked.Increment(ref _sequence) - 1;
            packet.Sequence = seq;
            packet.ResendTime = Environment.TickCount64 + ResendTime;
            
            _reliabilityQueue.TryAdd(seq, packet);
        }

        SendQueue.Enqueue(packet);
    }

    /// <summary>
    /// Adds a received packet to the buffer and processes it.
    /// </summary>
    /// <param name="packet">The received packet.</param>
    /// <returns>True if the packet was accepted, false otherwise.</returns>
    public bool AddToReceiveBuffer(VoiceCraftPacket packet)
    {
        LastActive = Environment.TickCount64;
        
        // Reject packets with invalid ID when connected
        if (State == NetPeerState.Connected && packet.Id != Id) 
            return false;

        if (!packet.IsReliable)
        {
            OnPacketReceived?.Invoke(this, packet);
            return true;
        }

        List<VoiceCraftPacket> packetsToProcess = [];

        lock (_receiveLock)
        {
            // Prevent buffer overflow attacks
            if (_receiveBuffer.Count >= MaxRecvBufferSize && packet.Sequence != _nextSequence)
                return false;
            
            // Send acknowledgment
            AddToSendBuffer(new Ack { PacketSequence = packet.Sequence });

            // Ignore duplicate packets
            if (packet.Sequence < _nextSequence) 
                return true;

            // Add to buffer (TryAdd won't replace existing)
            _receiveBuffer.TryAdd(packet.Sequence, packet);
            
            // Process sequential packets
            while (_receiveBuffer.TryRemove(_nextSequence, out var nextPacket))
            {
                _nextSequence++;
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

    /// <summary>
    /// Resends packets that have timed out waiting for acknowledgment.
    /// </summary>
    public void ResendPackets()
    {
        long now = Environment.TickCount64;

        foreach (var kvp in _reliabilityQueue)
        {
            var packet = kvp.Value;
            if (packet.ResendTime <= now)
            {
                // Update resend time
                packet.ResendTime = now + RetryResendTime;
                
                // Increment retries atomically
                Interlocked.Increment(ref packet.Retries);
                
                // Update Id since this might change on login
                packet.Id = Id;
                SendQueue.Enqueue(packet);
            }
        }
    }

    /// <summary>
    /// Accepts a login request and moves to Connected state.
    /// </summary>
    /// <param name="key">The session key to assign.</param>
    public void AcceptLogin(short key)
    {
        if (State == NetPeerState.Requesting)
        {
            AddToSendBuffer(new Accept { Key = key });
            _state = NetPeerState.Connected;
        }
    }

    /// <summary>
    /// Denies a login request with an optional reason.
    /// </summary>
    /// <param name="reason">The reason for denial.</param>
    public void DenyLogin(string? reason = null)
    {
        if (State == NetPeerState.Requesting)
        {
            AddToSendBuffer(new Deny { Reason = reason ?? string.Empty });
            DisconnectReason = reason;
            _state = NetPeerState.Disconnected;
        }
    }

    /// <summary>
    /// Disconnects this peer with an optional reason.
    /// </summary>
    /// <param name="reason">The reason for disconnection.</param>
    /// <param name="notify">Whether to notify the remote peer.</param>
    public void Disconnect(string? reason = null, bool notify = true)
    {
        if (State != NetPeerState.Disconnected)
        {
            if (notify)
                AddToSendBuffer(new Logout { Reason = reason ?? string.Empty });
            DisconnectReason = reason;
            _state = NetPeerState.Disconnected;
        }
    }

    /// <summary>
    /// Acknowledges receipt of a packet by removing it from the reliability queue.
    /// </summary>
    /// <param name="packetId">The sequence number of the acknowledged packet.</param>
    public void AcknowledgePacket(uint packetId)
    {
        _reliabilityQueue.TryRemove(packetId, out _);
    }

    /// <summary>
    /// Generates a unique random ID for a peer.
    /// </summary>
    /// <returns>A random long value suitable for peer identification.</returns>
    public static long GenerateId()
    {
        // long.MinValue is reserved to indicate no ID
        return Random.Shared.NextInt64(long.MinValue + 1, long.MaxValue);
    }

    /// <summary>
    /// Resets all buffers and state for reuse.
    /// </summary>
    public void Reset()
    {
        lock (_receiveLock)
        {
            SendQueue.Clear();
            _reliabilityQueue.Clear();
            _receiveBuffer.Clear();
            _nextSequence = 0;
            _sequence = 0;
        }
    }
}

/// <summary>
/// Represents the connection state of a network peer.
/// </summary>
public enum NetPeerState
{
    /// <summary>
    /// The peer is disconnected.
    /// </summary>
    Disconnected,
    
    /// <summary>
    /// The peer is requesting to connect.
    /// </summary>
    Requesting,
    
    /// <summary>
    /// The peer is connected and active.
    /// </summary>
    Connected
}

