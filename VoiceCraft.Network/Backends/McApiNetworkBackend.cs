using System;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Network.NetPeers;
using VoiceCraft.Network.Packets.McApiPackets;
using VoiceCraft.Network.Packets.McApiPackets.Event;
using VoiceCraft.Network.Packets.McApiPackets.Request;
using VoiceCraft.Network.Packets.McApiPackets.Response;

namespace VoiceCraft.Network.Backends;

public abstract class McApiNetworkBackend : IDisposable
{
    public abstract bool IsStarted { get; }
    public abstract int ConnectedPeersCount { get; }

    public abstract event Action<int>? OnStarted;
    public abstract event Action<string?>? OnStopped;
    public abstract event Action<McApiNetPeer>? OnLoginRequest;
    public abstract event Action<McApiNetPeer>? OnPeerConnected;
    public abstract event Action<McApiNetPeer, string?>? OnPeerDisconnected;
    public event Action<McApiNetPeer, IMcApiPacket>? OnNetworkReceive;

    ~McApiNetworkBackend()
    {
        Dispose(false);
    }

    public abstract void Start(int? port = null);
    public abstract void Update();
    public abstract void Stop();

    public abstract void Reject(McApiNetPeer netPeer, string? reason = null);

    public abstract void SendPacket<T>(McApiNetPeer netPeer, T packet,
        VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable) where T : IMcApiPacket;

    public abstract void Broadcast<T>(T packet,
        VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable) where T : IMcApiPacket;

    public abstract void Disconnect(McApiNetPeer netPeer, string? reason = null);
    public abstract void DisconnectAll(string? reason = null);

    public async Task<T> GetResponseAsync<T>(McApiNetPeer fromPeer, string requestId, TimeSpan timeout)
        where T : IMcApiPacket, IMcApiRIdPacket
    {
        var tcs = new TaskCompletionSource<T>();
        var dTcs = new TaskCompletionSource<string?>();
        using var cts = new CancellationTokenSource(timeout);
        cts.Token.Register(() => tcs.TrySetException(new TimeoutException()));

        OnNetworkReceive += EventCallback;
        OnPeerDisconnected += PeerDisconnectedCallback;
        try
        {
            var result = await Task.WhenAny(tcs.Task, dTcs.Task).ConfigureAwait(false);
            return result == tcs.Task ? tcs.Task.Result : throw new Exception(dTcs.Task.Result);
        }
        finally
        {
            OnNetworkReceive -= EventCallback;
        }

        void EventCallback(McApiNetPeer _, IMcApiPacket packet)
        {
            if (packet is not T typedPacket || typedPacket.RequestId != requestId) return;
            tcs.TrySetResult(typedPacket);
        }

        void PeerDisconnectedCallback(McApiNetPeer peer, string? reason)
        {
            if (peer != fromPeer) return;
            dTcs.TrySetResult(reason);
        }
    }

    public async Task<T> GetResponseAsync<T>(McApiNetPeer fromPeer, TimeSpan timeout) where T : IMcApiPacket
    {
        var tcs = new TaskCompletionSource<T>();
        var dTcs = new TaskCompletionSource<string?>();
        using var cts = new CancellationTokenSource(timeout);
        cts.Token.Register(() => tcs.TrySetException(new TimeoutException()));

        OnNetworkReceive += EventCallback;
        OnPeerDisconnected += PeerDisconnectedCallback;
        try
        {
            var result = await Task.WhenAny(tcs.Task, dTcs.Task).ConfigureAwait(false);
            return result == tcs.Task ? tcs.Task.Result : throw new Exception(dTcs.Task.Result);
        }
        finally
        {
            OnNetworkReceive -= EventCallback;
        }

        void EventCallback(McApiNetPeer _, IMcApiPacket packet)
        {
            if (packet is not T typedPacket) return;
            tcs.TrySetResult(typedPacket);
        }

        void PeerDisconnectedCallback(McApiNetPeer peer, string? reason)
        {
            if (peer != fromPeer) return;
            dTcs.TrySetResult(reason);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void ProcessPacket(McApiNetPeer netPeer, McApiPacketType packetType, NetDataReader reader)
    {
        switch (packetType)
        {
            case McApiPacketType.LoginRequest:
                ExecuteNetworkReceive<McApiLoginRequestPacket>(netPeer, reader);
                break;
            case McApiPacketType.LogoutRequest:
                ExecuteNetworkReceive<McApiLogoutRequestPacket>(netPeer, reader);
                break;
            case McApiPacketType.PingRequest:
                ExecuteNetworkReceive<McApiPingRequestPacket>(netPeer, reader);
                break;
            case McApiPacketType.AcceptResponse:
                ExecuteNetworkReceive<McApiAcceptResponsePacket>(netPeer, reader);
                break;
            case McApiPacketType.DenyResponse:
                ExecuteNetworkReceive<McApiDenyResponsePacket>(netPeer, reader);
                break;
            case McApiPacketType.PingResponse:
                ExecuteNetworkReceive<McApiPingResponsePacket>(netPeer, reader);
                break;
            case McApiPacketType.ResetRequest:
                ExecuteNetworkReceive<McApiResetRequestPacket>(netPeer, reader);
                break;
            case McApiPacketType.SetEffectRequest:
                ExecuteNetworkReceive<McApiSetEffectRequestPacket>(netPeer, reader);
                break;
            case McApiPacketType.ClearEffectsRequest:
                ExecuteNetworkReceive<McApiClearEffectsRequestPacket>(netPeer, reader);
                break;
            case McApiPacketType.CreateEntityRequest:
                ExecuteNetworkReceive<McApiCreateEntityRequestPacket>(netPeer, reader);
                break;
            case McApiPacketType.DestroyEntityRequest:
                ExecuteNetworkReceive<McApiDestroyEntityRequestPacket>(netPeer, reader);
                break;
            case McApiPacketType.EntityAudioRequest:
                ExecuteNetworkReceive<McApiEntityAudioRequestPacket>(netPeer, reader);
                break;
            case McApiPacketType.SetEntityTitleRequest:
                ExecuteNetworkReceive<McApiSetEntityTitleRequestPacket>(netPeer, reader);
                break;
            case McApiPacketType.SetEntityDescriptionRequest:
                ExecuteNetworkReceive<McApiSetEntityDescriptionRequestPacket>(netPeer, reader);
                break;
            case McApiPacketType.SetEntityWorldIdRequest:
                ExecuteNetworkReceive<McApiSetEntityWorldIdRequestPacket>(netPeer, reader);
                break;
            case McApiPacketType.SetEntityNameRequest:
                ExecuteNetworkReceive<McApiSetEntityNameRequestPacket>(netPeer, reader);
                break;
            case McApiPacketType.SetEntityMuteRequest:
                ExecuteNetworkReceive<McApiSetEntityMuteRequestPacket>(netPeer, reader);
                break;
            case McApiPacketType.SetEntityDeafenRequest:
                ExecuteNetworkReceive<McApiSetEntityDeafenRequestPacket>(netPeer, reader);
                break;
            case McApiPacketType.SetEntityTalkBitmaskRequest:
                ExecuteNetworkReceive<McApiSetEntityTalkBitmaskRequestPacket>(netPeer, reader);
                break;
            case McApiPacketType.SetEntityListenBitmaskRequest:
                ExecuteNetworkReceive<McApiSetEntityListenBitmaskRequestPacket>(netPeer, reader);
                break;
            case McApiPacketType.SetEntityEffectBitmaskRequest:
                ExecuteNetworkReceive<McApiSetEntityEffectBitmaskRequestPacket>(netPeer, reader);
                break;
            case McApiPacketType.SetEntityPositionRequest:
                ExecuteNetworkReceive<McApiSetEntityPositionRequestPacket>(netPeer, reader);
                break;
            case McApiPacketType.SetEntityRotationRequest:
                ExecuteNetworkReceive<McApiSetEntityRotationRequestPacket>(netPeer, reader);
                break;
            case McApiPacketType.SetEntityCaveFactorRequest:
                ExecuteNetworkReceive<McApiSetEntityCaveFactorRequestPacket>(netPeer, reader);
                break;
            case McApiPacketType.SetEntityMuffleFactorRequest:
                ExecuteNetworkReceive<McApiSetEntityMuffleFactorRequestPacket>(netPeer, reader);
                break;
            case McApiPacketType.ResetResponse:
                ExecuteNetworkReceive<McApiResetResponsePacket>(netPeer, reader);
                break;
            case McApiPacketType.CreateEntityResponse:
                ExecuteNetworkReceive<McApiCreateEntityResponsePacket>(netPeer, reader);
                break;
            case McApiPacketType.DestroyEntityResponse:
                ExecuteNetworkReceive<McApiDestroyEntityResponsePacket>(netPeer, reader);
                break;
            case McApiPacketType.OnEffectUpdated:
                ExecuteNetworkReceive<McApiOnEffectUpdatedPacket>(netPeer, reader);
                break;
            case McApiPacketType.OnEntityCreated:
                ExecuteNetworkReceive<McApiOnEntityCreatedPacket>(netPeer, reader);
                break;
            case McApiPacketType.OnNetworkEntityCreated:
                ExecuteNetworkReceive<McApiOnNetworkEntityCreatedPacket>(netPeer, reader);
                break;
            case McApiPacketType.OnEntityDestroyed:
                ExecuteNetworkReceive<McApiOnEntityDestroyedPacket>(netPeer, reader);
                break;
            case McApiPacketType.OnEntityVisibilityUpdated:
                ExecuteNetworkReceive<McApiOnEntityVisibilityUpdatedPacket>(netPeer, reader);
                break;
            case McApiPacketType.OnEntityWorldIdUpdated:
                ExecuteNetworkReceive<McApiOnEntityWorldIdUpdatedPacket>(netPeer, reader);
                break;
            case McApiPacketType.OnEntityNameUpdated:
                ExecuteNetworkReceive<McApiOnEntityNameUpdatedPacket>(netPeer, reader);
                break;
            case McApiPacketType.OnEntityMuteUpdated:
                ExecuteNetworkReceive<McApiOnEntityMuteUpdatedPacket>(netPeer, reader);
                break;
            case McApiPacketType.OnEntityDeafenUpdated:
                ExecuteNetworkReceive<McApiOnEntityDeafenUpdatedPacket>(netPeer, reader);
                break;
            case McApiPacketType.OnEntityServerMuteUpdated:
                ExecuteNetworkReceive<McApiOnEntityServerMuteUpdatedPacket>(netPeer, reader);
                break;
            case McApiPacketType.OnEntityServerDeafenUpdated:
                ExecuteNetworkReceive<McApiOnEntityServerDeafenUpdatedPacket>(netPeer, reader);
                break;
            case McApiPacketType.OnEntityTalkBitmaskUpdated:
                ExecuteNetworkReceive<McApiOnEntityTalkBitmaskUpdatedPacket>(netPeer, reader);
                break;
            case McApiPacketType.OnEntityListenBitmaskUpdated:
                ExecuteNetworkReceive<McApiOnEntityListenBitmaskUpdatedPacket>(netPeer, reader);
                break;
            case McApiPacketType.OnEntityEffectBitmaskUpdated:
                ExecuteNetworkReceive<McApiOnEntityEffectBitmaskUpdatedPacket>(netPeer, reader);
                break;
            case McApiPacketType.OnEntityPositionUpdated:
                ExecuteNetworkReceive<McApiOnEntityPositionUpdatedPacket>(netPeer, reader);
                break;
            case McApiPacketType.OnEntityRotationUpdated:
                ExecuteNetworkReceive<McApiOnEntityRotationUpdatedPacket>(netPeer, reader);
                break;
            case McApiPacketType.OnEntityCaveFactorUpdated:
                ExecuteNetworkReceive<McApiOnEntityCaveFactorUpdatedPacket>(netPeer, reader);
                break;
            case McApiPacketType.OnEntityMuffleFactorUpdated:
                ExecuteNetworkReceive<McApiOnEntityMuffleFactorUpdatedPacket>(netPeer, reader);
                break;
            case McApiPacketType.OnEntityAudioReceived:
                ExecuteNetworkReceive<McApiOnEntityAudioReceivedPacket>(netPeer, reader);
                break;
            default:
                return;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        OnNetworkReceive = null;
    }

    private void ExecuteNetworkReceive<T>(McApiNetPeer netPeer, NetDataReader reader) where T : IMcApiPacket
    {
        var packet = PacketPool<T>.GetPacket();
        try
        {
            packet.Deserialize(reader);
            OnNetworkReceive?.Invoke(netPeer, packet);
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }
}