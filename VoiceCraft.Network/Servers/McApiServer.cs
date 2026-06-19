using System;
using System.Collections.Immutable;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Core.World;
using VoiceCraft.Network.NetPeers;
using VoiceCraft.Network.Packets.McApiPackets;
using VoiceCraft.Network.Packets.McApiPackets.Request;
using VoiceCraft.Network.Packets.McApiPackets.Response;
using VoiceCraft.Network.Systems;
using VoiceCraft.Network.World;

namespace VoiceCraft.Network.Servers;

public abstract class McApiServer(VoiceCraftWorld world, AudioEffectSystem audioEffectSystem) : IDisposable
{
    protected bool Disposed;

    public static Version Version { get; } = new(Constants.Major, Constants.Minor, Constants.Patch);
    public abstract string LoginToken { get; }
    public abstract uint MaxClients { get; }
    public abstract int ConnectedPeers { get; }
    public abstract ImmutableList<McApiNetPeer> Peers { get; }

    public abstract event Action<McApiNetPeer, string>? OnPeerConnected;
    public abstract event Action<McApiNetPeer, string>? OnPeerDisconnected;

    public abstract void Start();

    public abstract void Update();

    public abstract void Stop();

    public abstract void SendPacket<T>(McApiNetPeer netPeer, T packet) where T : IMcApiPacket;

    public abstract void Broadcast<T>(T packet, params McApiNetPeer?[] excludes) where T : IMcApiPacket;

    public abstract void Disconnect(McApiNetPeer netPeer, bool force = false);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected abstract void AcceptRequest(McApiLoginRequestPacket packet, McApiNetPeer netPeer);

    protected abstract void RejectRequest(McApiLoginRequestPacket packet, string reason, McApiNetPeer netPeer);

    protected static void ProcessPacket(NetDataReader reader, Action<IMcApiPacket> onParsed)
    {
        var packetType = (McApiPacketType)reader.GetByte();
        switch (packetType)
        {
            case McApiPacketType.LoginRequest:
                ProcessPacket(reader, onParsed, () => new McApiLoginRequestPacket());
                break;
            case McApiPacketType.LogoutRequest:
                ProcessPacket(reader, onParsed, () => new McApiLogoutRequestPacket());
                break;
            case McApiPacketType.PingRequest:
                ProcessPacket(reader, onParsed, () => new McApiPingRequestPacket());
                break;
            case McApiPacketType.ResetRequest:
                ProcessPacket(reader, onParsed, () => new McApiResetRequestPacket());
                break;
            case McApiPacketType.SetEffectRequest:
                ProcessPacket(reader, onParsed, () => new McApiSetEffectRequestPacket());
                break;
            case McApiPacketType.ClearEffectsRequest:
                ProcessPacket(reader, onParsed, () => new McApiClearEffectsRequestPacket());
                break;
            case McApiPacketType.CreateEntityRequest:
                ProcessPacket(reader, onParsed, () => new McApiCreateEntityRequestPacket());
                break;
            case McApiPacketType.DestroyEntityRequest:
                ProcessPacket(reader, onParsed, () => new McApiDestroyEntityRequestPacket());
                break;
            case McApiPacketType.EntityAudioRequest:
                ProcessPacket(reader, onParsed, () => new McApiEntityAudioRequestPacket());
                break;
            case McApiPacketType.SetEntityTitleRequest:
                ProcessPacket(reader, onParsed, () => new McApiSetEntityTitleRequestPacket());
                break;
            case McApiPacketType.SetEntityDescriptionRequest:
                ProcessPacket(reader, onParsed, () => new McApiSetEntityDescriptionRequestPacket());
                break;
            case McApiPacketType.SetEntityWorldIdRequest:
                ProcessPacket(reader, onParsed, () => new McApiSetEntityWorldIdRequestPacket());
                break;
            case McApiPacketType.SetEntityNameRequest:
                ProcessPacket(reader, onParsed, () => new McApiSetEntityNameRequestPacket());
                break;
            case McApiPacketType.SetEntityMuteRequest:
                ProcessPacket(reader, onParsed, () => new McApiSetEntityMuteRequestPacket());
                break;
            case McApiPacketType.SetEntityDeafenRequest:
                ProcessPacket(reader, onParsed, () => new McApiSetEntityDeafenRequestPacket());
                break;
            case McApiPacketType.SetEntityTalkBitmaskRequest:
                ProcessPacket(reader, onParsed, () => new McApiSetEntityTalkBitmaskRequestPacket());
                break;
            case McApiPacketType.SetEntityListenBitmaskRequest:
                ProcessPacket(reader, onParsed, () => new McApiSetEntityListenBitmaskRequestPacket());
                break;
            case McApiPacketType.SetEntityEffectBitmaskRequest:
                ProcessPacket(reader, onParsed, () => new McApiSetEntityEffectBitmaskRequestPacket());
                break;
            case McApiPacketType.SetEntityPositionRequest:
                ProcessPacket(reader, onParsed, () => new McApiSetEntityPositionRequestPacket());
                break;
            case McApiPacketType.SetEntityRotationRequest:
                ProcessPacket(reader, onParsed, () => new McApiSetEntityRotationRequestPacket());
                break;
            case McApiPacketType.SetEntityPropertyRequest:
                ProcessPacket(reader, onParsed, () => new McApiSetEntityPropertyRequestPacket());
                break;
            case McApiPacketType.AcceptResponse:
            case McApiPacketType.DenyResponse:
            case McApiPacketType.PingResponse:
            case McApiPacketType.EventRequest:
            case McApiPacketType.ResetResponse:
            case McApiPacketType.CreateEntityResponse:
            case McApiPacketType.DestroyEntityResponse:
            default:
                return;
        }
    }

    protected void ExecutePacket(IMcApiPacket packet, McApiNetPeer netPeer)
    {
        switch (packet)
        {
            case McApiLoginRequestPacket loginRequestPacket:
                HandleLoginRequestPacket(loginRequestPacket, netPeer);
                break;
            case McApiLogoutRequestPacket logoutRequestPacket:
                HandleLogoutRequestPacket(logoutRequestPacket, netPeer);
                break;
            case McApiPingRequestPacket pingRequestPacket:
                HandlePingRequestPacket(pingRequestPacket, netPeer);
                break;
            case McApiResetRequestPacket resetRequestPacket:
                HandleResetRequestPacket(resetRequestPacket, netPeer);
                break;
            case McApiSetEffectRequestPacket setEffectRequestPacket:
                HandleSetEffectRequestPacket(setEffectRequestPacket, netPeer);
                break;
            case McApiClearEffectsRequestPacket clearEffectsRequestPacket:
                HandleClearEffectsRequestPacket(clearEffectsRequestPacket, netPeer);
                break;
            case McApiCreateEntityRequestPacket createEntityRequestPacket:
                HandleCreateEntityRequestPacket(createEntityRequestPacket, netPeer);
                break;
            case McApiDestroyEntityRequestPacket destroyEntityRequestPacket:
                HandleDestroyEntityRequestPacket(destroyEntityRequestPacket, netPeer);
                break;
            case McApiEntityAudioRequestPacket entityAudioRequestPacket:
                HandleEntityAudioRequestPacket(entityAudioRequestPacket, netPeer);
                break;
            case McApiSetEntityTitleRequestPacket setEntityTitleRequestPacket:
                HandleSetEntityTitleRequestPacket(setEntityTitleRequestPacket, netPeer);
                break;
            case McApiSetEntityDescriptionRequestPacket setEntityDescriptionRequestPacket:
                HandleSetEntityDescriptionRequestPacket(setEntityDescriptionRequestPacket, netPeer);
                break;
            case McApiSetEntityWorldIdRequestPacket setEntityWorldIdRequestPacket:
                HandleSetEntityWorldIdRequestPacket(setEntityWorldIdRequestPacket, netPeer);
                break;
            case McApiSetEntityNameRequestPacket setEntityNameRequestPacket:
                HandleSetEntityNameRequestPacket(setEntityNameRequestPacket, netPeer);
                break;
            case McApiSetEntityMuteRequestPacket setEntityMuteRequestPacket:
                HandleSetEntityMuteRequestPacket(setEntityMuteRequestPacket, netPeer);
                break;
            case McApiSetEntityDeafenRequestPacket setEntityDeafenRequestPacket:
                HandleSetEntityDeafenRequestPacket(setEntityDeafenRequestPacket, netPeer);
                break;
            case McApiSetEntityTalkBitmaskRequestPacket setEntityTalkBitmaskRequestPacket:
                HandleSetEntityTalkBitmaskRequestPacket(setEntityTalkBitmaskRequestPacket, netPeer);
                break;
            case McApiSetEntityListenBitmaskRequestPacket setEntityListenBitmaskRequestPacket:
                HandleSetEntityListenBitmaskRequestPacket(setEntityListenBitmaskRequestPacket, netPeer);
                break;
            case McApiSetEntityEffectBitmaskRequestPacket setEntityEffectBitmaskRequestPacket:
                HandleSetEntityEffectBitmaskRequestPacket(setEntityEffectBitmaskRequestPacket, netPeer);
                break;
            case McApiSetEntityPositionRequestPacket setEntityPositionRequestPacket:
                HandleSetEntityPositionRequestPacket(setEntityPositionRequestPacket, netPeer);
                break;
            case McApiSetEntityRotationRequestPacket setEntityRotationRequestPacket:
                HandleSetEntityRotationRequestPacket(setEntityRotationRequestPacket, netPeer);
                break;
            case McApiSetEntityPropertyRequestPacket setEntityPropertyRequestPacket:
                HandleSetEntityPropertyRequestPacket(setEntityPropertyRequestPacket, netPeer);
                break;
        }
    }

    protected static bool AuthorizePacket(IMcApiPacket packet, McApiNetPeer netPeer, string token)
    {
        return packet switch
        {
            McApiLoginRequestPacket => true,
            McApiLogoutRequestPacket => true,
            _ => netPeer.ConnectionState == McApiConnectionState.Connected && token == netPeer.SessionToken
        };
    }

    protected virtual void Dispose(bool disposing)
    {
        if (Disposed) return;
        if (disposing)
        {
            Stop();
        }

        Disposed = true;
    }

    private void HandleLoginRequestPacket(McApiLoginRequestPacket packet, McApiNetPeer netPeer)
    {
        if (netPeer.ConnectionState == McApiConnectionState.Connected)
        {
            AcceptRequest(packet, netPeer);
            //Sync Event Subscriptions
            netPeer.SubscribedEvents.Clear();
            foreach (var @event in packet.SubscribeEvents)
            {
                netPeer.SubscribedEvents.Add(@event);
            }
            return;
        }

        netPeer.ConnectionState = McApiConnectionState.Connecting;

        if (ConnectedPeers >= MaxClients)
        {
            RejectRequest(packet, "VcMcApi.DisconnectReason.ServerFull", netPeer);
            return;
        }

        if (!string.IsNullOrEmpty(LoginToken) && LoginToken != packet.Token)
        {
            RejectRequest(packet, "VcMcApi.DisconnectReason.InvalidLoginToken", netPeer);
            return;
        }

        if (packet.Version.Major != Version.Major || packet.Version.Minor != Version.Minor)
        {
            RejectRequest(packet, "VcMcApi.DisconnectReason.IncompatibleVersion", netPeer);
            return;
        }

        AcceptRequest(packet, netPeer);
        //Sync Event Subscriptions
        netPeer.SubscribedEvents.Clear();
        foreach (var @event in packet.SubscribeEvents)
        {
            netPeer.SubscribedEvents.Add(@event);
        }
    }

    private void HandleLogoutRequestPacket(McApiLogoutRequestPacket packet, McApiNetPeer netPeer)
    {
        if (packet.Token != netPeer.SessionToken) return;
        Disconnect(netPeer, true);
    }

    private void HandlePingRequestPacket(McApiPingRequestPacket _, McApiNetPeer netPeer)
    {
        SendPacket(netPeer, PacketPool<McApiPingResponsePacket>.GetPacket(() => new McApiPingResponsePacket()).Set());
    }

    private void HandleResetRequestPacket(McApiResetRequestPacket packet, McApiNetPeer netPeer)
    {
        if (netPeer.ConnectionState != McApiConnectionState.Connected) return;
        try
        {
            world.Reset();
            audioEffectSystem.Reset();
            SendPacket(netPeer,
                PacketPool<McApiResetResponsePacket>.GetPacket(() => new McApiResetResponsePacket())
                    .Set(packet.RequestId));
        }
        catch
        {
            SendPacket(netPeer, PacketPool<McApiResetResponsePacket>.GetPacket(() => new McApiResetResponsePacket())
                .Set(packet.RequestId, McApiResetResponsePacket.ResponseCodes.Failure));
        }
    }

    private void HandleSetEffectRequestPacket(McApiSetEffectRequestPacket packet, McApiNetPeer netPeer)
    {
        if (netPeer.ConnectionState != McApiConnectionState.Connected) return;
        audioEffectSystem.SetEffect(packet.Bitmask, packet.Effect);
    }

    private void HandleClearEffectsRequestPacket(McApiClearEffectsRequestPacket _, McApiNetPeer netPeer)
    {
        if (netPeer.ConnectionState != McApiConnectionState.Connected) return;
        audioEffectSystem.ClearEffects();
    }

    private void HandleCreateEntityRequestPacket(McApiCreateEntityRequestPacket packet, McApiNetPeer netPeer)
    {
        if (netPeer.ConnectionState != McApiConnectionState.Connected) return;
        try
        {
            var entityId = world.GetNextId();
            var entity = new VoiceCraftEntity(entityId)
            {
                WorldId = packet.WorldId,
                Name = packet.Name,
                Muted = packet.Muted,
                Deafened = packet.Deafened,
                TalkBitmask = packet.TalkBitmask,
                ListenBitmask = packet.ListenBitmask,
                EffectBitmask = packet.EffectBitmask,
                Position = packet.Position,
                Rotation = packet.Rotation
            };

            world.AddEntity(entity);
            SendPacket(netPeer, PacketPool<McApiCreateEntityResponsePacket>
                .GetPacket(() => new McApiCreateEntityResponsePacket())
                .Set(packet.RequestId, McApiCreateEntityResponsePacket.ResponseCodes.Ok, entity.Id));
        }
        catch
        {
            SendPacket(netPeer,
                PacketPool<McApiCreateEntityResponsePacket>.GetPacket(() => new McApiCreateEntityResponsePacket()).Set(
                    packet.RequestId,
                    McApiCreateEntityResponsePacket.ResponseCodes.Failure));
        }
    }

    private void HandleDestroyEntityRequestPacket(McApiDestroyEntityRequestPacket packet, McApiNetPeer netPeer)
    {
        if (netPeer.ConnectionState != McApiConnectionState.Connected) return;
        try
        {
            world.DestroyEntity(packet.Id);
            SendPacket(netPeer, PacketPool<McApiDestroyEntityResponsePacket>
                .GetPacket(() => new McApiDestroyEntityResponsePacket())
                .Set(packet.RequestId));
        }
        catch
        {
            SendPacket(netPeer, PacketPool<McApiDestroyEntityResponsePacket>
                .GetPacket(() => new McApiDestroyEntityResponsePacket())
                .Set(packet.RequestId, McApiDestroyEntityResponsePacket.ResponseCodes.NotFound));
        }
    }

    private void HandleEntityAudioRequestPacket(McApiEntityAudioRequestPacket packet, McApiNetPeer netPeer)
    {
        if (netPeer.ConnectionState != McApiConnectionState.Connected) return;
        var entity = world.GetEntity(packet.Id);
        if (entity is null or VoiceCraftNetworkEntity) return;
        entity.ReceiveAudio(packet.Buffer, packet.Timestamp, packet.FrameLoudness);
    }

    private void HandleSetEntityTitleRequestPacket(McApiSetEntityTitleRequestPacket packet, McApiNetPeer netPeer)
    {
        if (netPeer.ConnectionState != McApiConnectionState.Connected) return;
        var entity = world.GetEntity(packet.Id);
        if (entity is not VoiceCraftNetworkEntity networkEntity) return;
        networkEntity.SetTitle(packet.Value);
    }

    private void HandleSetEntityDescriptionRequestPacket(McApiSetEntityDescriptionRequestPacket packet,
        McApiNetPeer netPeer)
    {
        if (netPeer.ConnectionState != McApiConnectionState.Connected) return;
        var entity = world.GetEntity(packet.Id);
        if (entity is not VoiceCraftNetworkEntity networkEntity) return;
        networkEntity.SetDescription(packet.Value);
    }

    private void HandleSetEntityWorldIdRequestPacket(McApiSetEntityWorldIdRequestPacket packet, McApiNetPeer netPeer)
    {
        if (netPeer.ConnectionState != McApiConnectionState.Connected) return;
        var entity = world.GetEntity(packet.Id);
        if (entity is null or VoiceCraftNetworkEntity { PositioningType: PositioningType.Client }) return;
        entity.WorldId = packet.Value;
    }

    private void HandleSetEntityNameRequestPacket(McApiSetEntityNameRequestPacket packet, McApiNetPeer netPeer)
    {
        if (netPeer.ConnectionState != McApiConnectionState.Connected) return;
        var entity = world.GetEntity(packet.Id);
        if (entity is null or VoiceCraftNetworkEntity { PositioningType: PositioningType.Client }) return;
        entity.Name = packet.Value;
    }

    private void HandleSetEntityMuteRequestPacket(McApiSetEntityMuteRequestPacket packet, McApiNetPeer netPeer)
    {
        if (netPeer.ConnectionState != McApiConnectionState.Connected) return;
        var entity = world.GetEntity(packet.Id);
        if (entity == null) return;
        switch (entity)
        {
            case VoiceCraftNetworkEntity networkEntity:
                networkEntity.ServerMuted = packet.Value;
                break;
            default:
                entity.Muted = packet.Value;
                break;
        }
    }

    private void HandleSetEntityDeafenRequestPacket(McApiSetEntityDeafenRequestPacket packet, McApiNetPeer netPeer)
    {
        if (netPeer.ConnectionState != McApiConnectionState.Connected) return;
        var entity = world.GetEntity(packet.Id);
        if (entity == null) return;
        switch (entity)
        {
            case VoiceCraftNetworkEntity networkEntity:
                networkEntity.ServerDeafened = packet.Value;
                break;
            default:
                entity.Deafened = packet.Value;
                break;
        }
    }

    private void HandleSetEntityTalkBitmaskRequestPacket(McApiSetEntityTalkBitmaskRequestPacket packet,
        McApiNetPeer netPeer)
    {
        if (netPeer.ConnectionState != McApiConnectionState.Connected) return;
        var entity = world.GetEntity(packet.Id);
        entity?.TalkBitmask = packet.Value;
    }

    private void HandleSetEntityListenBitmaskRequestPacket(McApiSetEntityListenBitmaskRequestPacket packet,
        McApiNetPeer netPeer)
    {
        if (netPeer.ConnectionState != McApiConnectionState.Connected) return;
        var entity = world.GetEntity(packet.Id);
        entity?.ListenBitmask = packet.Value;
    }

    private void HandleSetEntityEffectBitmaskRequestPacket(McApiSetEntityEffectBitmaskRequestPacket packet,
        McApiNetPeer netPeer)
    {
        if (netPeer.ConnectionState != McApiConnectionState.Connected) return;
        var entity = world.GetEntity(packet.Id);
        entity?.EffectBitmask = packet.Value;
    }

    private void HandleSetEntityPositionRequestPacket(McApiSetEntityPositionRequestPacket packet, McApiNetPeer netPeer)
    {
        if (netPeer.ConnectionState != McApiConnectionState.Connected) return;
        var entity = world.GetEntity(packet.Id);
        if (entity is null or VoiceCraftNetworkEntity { PositioningType: PositioningType.Client }) return;
        entity.Position = packet.Value;
    }

    private void HandleSetEntityRotationRequestPacket(McApiSetEntityRotationRequestPacket packet, McApiNetPeer netPeer)
    {
        if (netPeer.ConnectionState != McApiConnectionState.Connected) return;
        var entity = world.GetEntity(packet.Id);
        if (entity is null or VoiceCraftNetworkEntity { PositioningType: PositioningType.Client }) return;
        entity.Rotation = packet.Value;
    }

    private void HandleSetEntityPropertyRequestPacket(McApiSetEntityPropertyRequestPacket packet, McApiNetPeer netPeer)
    {
        if (netPeer.ConnectionState != McApiConnectionState.Connected) return;
        var entity = world.GetEntity(packet.Id);
        if (entity is null or VoiceCraftNetworkEntity { PositioningType: PositioningType.Client }) return;
        entity.SetProperty(packet.Key, packet.Value);
    }

    private static void ProcessPacket<T>(NetDataReader reader, Action<IMcApiPacket> onParsed, Func<T> packetFactory)
        where T : IMcApiPacket
    {
        var packet = PacketPool<T>.GetPacket(packetFactory);
        try
        {
            packet.Deserialize(reader);
            onParsed.Invoke(packet);
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }
}
