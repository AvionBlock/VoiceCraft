using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace VoiceCraft.Network.NetPeers;

public class TcpMcApiNetPeer(TcpClient client) : McApiNetPeer
{
    private McApiConnectionState _connectionState;
    private string _sessionToken = string.Empty;
    private readonly object _responseLock = new();
    private TaskCompletionSource<List<byte[]>>? _pendingResponse;

    public TcpClient Client { get; } = client;
    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    public ConcurrentQueue<QueuedRawPacket> IncomingRawQueue { get; } = new();
    public ConcurrentQueue<byte[]> OutgoingRawQueue { get; } = new();
    public override McApiConnectionState ConnectionState => _connectionState;
    public override string SessionToken => _sessionToken;

    public void SetConnectionState(McApiConnectionState state)
    {
        _connectionState = state;
    }

    public void SetSessionToken(string token)
    {
        _sessionToken = token;
    }

    public Task<List<byte[]>> CreatePendingResponseTask()
    {
        lock (_responseLock)
        {
            _pendingResponse ??= new TaskCompletionSource<List<byte[]>>(TaskCreationOptions.RunContinuationsAsynchronously);
            return _pendingResponse.Task;
        }
    }

    public void CompletePendingResponse(List<byte[]> packets)
    {
        TaskCompletionSource<List<byte[]>>? pendingResponse;
        lock (_responseLock)
        {
            pendingResponse = _pendingResponse;
            _pendingResponse = null;
        }

        pendingResponse?.TrySetResult(packets);
    }

    public bool HasPendingResponse()
    {
        lock (_responseLock)
        {
            return _pendingResponse != null;
        }
    }

    public void CancelPendingResponse()
    {
        TaskCompletionSource<List<byte[]>>? pendingResponse;
        lock (_responseLock)
        {
            pendingResponse = _pendingResponse;
            _pendingResponse = null;
        }

        pendingResponse?.TrySetCanceled();
    }

    public readonly struct QueuedRawPacket(byte[] data, string token)
    {
        public byte[] Data { get; } = data;
        public string Token { get; } = token;
    }
}
