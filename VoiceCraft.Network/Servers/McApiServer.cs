using System;
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

    protected abstract void AcceptRequest(McApiLoginRequestPacket packet, object? data);

    protected abstract void RejectRequest(McApiLoginRequestPacket packet, string reason, object? data);

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
            case McApiPacketType.SetEntityCaveFactorRequest:
                ProcessPacket(reader, onParsed, () => new McApiSetEntityCaveFactorRequestPacket());
                break;
            case McApiPacketType.SetEntityMuffleFactorRequest:
                ProcessPacket(reader, onParsed, () => new McApiSetEntityMuffleFactorRequestPacket());
                break;
            case McApiPacketType.AcceptResponse:
            case McApiPacketType.DenyResponse:
            case McApiPacketType.PingResponse:
            case McApiPacketType.ResetResponse:
            case McApiPacketType.CreateEntityResponse:
            case McApiPacketType.DestroyEntityResponse:
            case McApiPacketType.OnEffectUpdated:
            case McApiPacketType.OnEntityCreated:
            case McApiPacketType.OnNetworkEntityCreated:
            case McApiPacketType.OnEntityDestroyed:
            case McApiPacketType.OnEntityVisibilityUpdated:
            case McApiPacketType.OnEntityWorldIdUpdated:
            case McApiPacketType.OnEntityNameUpdated:
            case McApiPacketType.OnEntityMuteUpdated:
            case McApiPacketType.OnEntityDeafenUpdated:
            case McApiPacketType.OnEntityServerMuteUpdated:
            case McApiPacketType.OnEntityServerDeafenUpdated:
            case McApiPacketType.OnEntityTalkBitmaskUpdated:
            case McApiPacketType.OnEntityListenBitmaskUpdated:
            case McApiPacketType.OnEntityEffectBitmaskUpdated:
            case McApiPacketType.OnEntityPositionUpdated:
            case McApiPacketType.OnEntityRotationUpdated:
            case McApiPacketType.OnEntityCaveFactorUpdated:
            case McApiPacketType.OnEntityMuffleFactorUpdated:
            case McApiPacketType.OnEntityAudioReceived:
            default:
                return;
        }
    }

    protected void ExecutePacket(IMcApiPacket packet, object? data)
    {
        switch (packet)
        {
            case McApiLoginRequestPacket loginRequestPacket:
                HandleLoginRequestPacket(loginRequestPacket, data);
                break;
            case McApiLogoutRequestPacket logoutRequestPacket:
                HandleLogoutRequestPacket(logoutRequestPacket, data);
                break;
            case McApiPingRequestPacket pingRequestPacket:
                HandlePingRequestPacket(pingRequestPacket, data);
                break;
            case McApiResetRequestPacket resetRequestPacket:
                HandleResetRequestPacket(resetRequestPacket, data);
                break;
            case McApiSetEffectRequestPacket setEffectRequestPacket:
                HandleSetEffectRequestPacket(setEffectRequestPacket, data);
                break;
            case McApiClearEffectsRequestPacket clearEffectsRequestPacket:
                HandleClearEffectsRequestPacket(clearEffectsRequestPacket, data);
                break;
            case McApiCreateEntityRequestPacket createEntityRequestPacket:
                HandleCreateEntityRequestPacket(createEntityRequestPacket, data);
                break;
            case McApiDestroyEntityRequestPacket destroyEntityRequestPacket:
                HandleDestroyEntityRequestPacket(destroyEntityRequestPacket, data);
                break;
            case McApiEntityAudioRequestPacket entityAudioRequestPacket:
                HandleEntityAudioRequestPacket(entityAudioRequestPacket, data);
                break;
            case McApiSetEntityTitleRequestPacket setEntityTitleRequestPacket:
                HandleSetEntityTitleRequestPacket(setEntityTitleRequestPacket, data);
                break;
            case McApiSetEntityDescriptionRequestPacket setEntityDescriptionRequestPacket:
                HandleSetEntityDescriptionRequestPacket(setEntityDescriptionRequestPacket, data);
                break;
            case McApiSetEntityWorldIdRequestPacket setEntityWorldIdRequestPacket:
                HandleSetEntityWorldIdRequestPacket(setEntityWorldIdRequestPacket, data);
                break;
            case McApiSetEntityNameRequestPacket setEntityNameRequestPacket:
                HandleSetEntityNameRequestPacket(setEntityNameRequestPacket, data);
                break;
            case McApiSetEntityMuteRequestPacket setEntityMuteRequestPacket:
                HandleSetEntityMuteRequestPacket(setEntityMuteRequestPacket, data);
                break;
            case McApiSetEntityDeafenRequestPacket setEntityDeafenRequestPacket:
                HandleSetEntityDeafenRequestPacket(setEntityDeafenRequestPacket, data);
                break;
            case McApiSetEntityTalkBitmaskRequestPacket setEntityTalkBitmaskRequestPacket:
                HandleSetEntityTalkBitmaskRequestPacket(setEntityTalkBitmaskRequestPacket, data);
                break;
            case McApiSetEntityListenBitmaskRequestPacket setEntityListenBitmaskRequestPacket:
                HandleSetEntityListenBitmaskRequestPacket(setEntityListenBitmaskRequestPacket, data);
                break;
            case McApiSetEntityEffectBitmaskRequestPacket setEntityEffectBitmaskRequestPacket:
                HandleSetEntityEffectBitmaskRequestPacket(setEntityEffectBitmaskRequestPacket, data);
                break;
            case McApiSetEntityPositionRequestPacket setEntityPositionRequestPacket:
                HandleSetEntityPositionRequestPacket(setEntityPositionRequestPacket, data);
                break;
            case McApiSetEntityRotationRequestPacket setEntityRotationRequestPacket:
                HandleSetEntityRotationRequestPacket(setEntityRotationRequestPacket, data);
                break;
            case McApiSetEntityCaveFactorRequestPacket setEntityCaveFactorRequestPacket:
                HandleSetEntityCaveFactorRequestPacket(setEntityCaveFactorRequestPacket, data);
                break;
            case McApiSetEntityMuffleFactorRequestPacket setEntityMuffleFactorRequestPacket:
                HandleSetEntityMuffleFactorRequestPacket(setEntityMuffleFactorRequestPacket, data);
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

    private void HandleLoginRequestPacket(McApiLoginRequestPacket packet, object? data)
    {
        if (data is not McApiNetPeer netPeer) return;
        if (netPeer.ConnectionState == McApiConnectionState.Connected)
        {
            AcceptRequest(packet, data);
            return;
        }

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

        AcceptRequest(packet, data);
    }

    private void HandleLogoutRequestPacket(McApiLogoutRequestPacket packet, object? data)
    {
        if (data is not McApiNetPeer netPeer) return;
        if (packet.Token != netPeer.SessionToken) return;
        Disconnect(netPeer, true);
    }

    private void HandlePingRequestPacket(McApiPingRequestPacket _, object? data)
    {
        if (data is not McApiNetPeer netPeer) return;
        SendPacket(netPeer, PacketPool<McApiPingResponsePacket>.GetPacket(() => new McApiPingResponsePacket()).Set());
    }

    private void HandleResetRequestPacket(McApiResetRequestPacket packet, object? data)
    {
        if (data is not McApiNetPeer { ConnectionState: McApiConnectionState.Connected } netPeer) return;
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

    private void HandleSetEffectRequestPacket(McApiSetEffectRequestPacket packet, object? data)
    {
        if (data is not McApiNetPeer { ConnectionState: McApiConnectionState.Connected }) return;
        audioEffectSystem.SetEffect(packet.Bitmask, packet.Effect);
    }

    private void HandleClearEffectsRequestPacket(McApiClearEffectsRequestPacket _, object? data)
    {
        if (data is not McApiNetPeer { ConnectionState: McApiConnectionState.Connected }) return;
        audioEffectSystem.ClearEffects();
    }

    private void HandleCreateEntityRequestPacket(McApiCreateEntityRequestPacket packet, object? data)
    {
        if (data is not McApiNetPeer { ConnectionState: McApiConnectionState.Connected } netPeer) return;
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
                Rotation = packet.Rotation,
                CaveFactor = packet.CaveFactor,
                MuffleFactor = packet.MuffleFactor
            };
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

    private void HandleDestroyEntityRequestPacket(McApiDestroyEntityRequestPacket packet, object? data)
    {
        if (data is not McApiNetPeer { ConnectionState: McApiConnectionState.Connected } netPeer) return;
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

    private void HandleEntityAudioRequestPacket(McApiEntityAudioRequestPacket packet, object? data)
    {
        if (data is not McApiNetPeer { ConnectionState: McApiConnectionState.Connected }) return;
        var entity = world.GetEntity(packet.Id);
        if (entity is null or VoiceCraftNetworkEntity) return;
        entity.ReceiveAudio(packet.Buffer, packet.Timestamp, packet.FrameLoudness);
    }

    private void HandleSetEntityTitleRequestPacket(McApiSetEntityTitleRequestPacket packet, object? data)
    {
        if (data is not McApiNetPeer { ConnectionState: McApiConnectionState.Connected }) return;
        var entity = world.GetEntity(packet.Id);
        if (entity is not VoiceCraftNetworkEntity networkEntity) return;
        networkEntity.SetTitle(packet.Value);
    }

    private void HandleSetEntityDescriptionRequestPacket(McApiSetEntityDescriptionRequestPacket packet, object? data)
    {
        if (data is not McApiNetPeer { ConnectionState: McApiConnectionState.Connected }) return;
        var entity = world.GetEntity(packet.Id);
        if (entity is not VoiceCraftNetworkEntity networkEntity) return;
        networkEntity.SetDescription(packet.Value);
    }

    private void HandleSetEntityWorldIdRequestPacket(McApiSetEntityWorldIdRequestPacket packet, object? data)
    {
        if (data is not McApiNetPeer { ConnectionState: McApiConnectionState.Connected }) return;
        var entity = world.GetEntity(packet.Id);
        if (entity is null or VoiceCraftNetworkEntity { PositioningType: PositioningType.Client }) return;
        entity.WorldId = packet.Value;
    }

    private void HandleSetEntityNameRequestPacket(McApiSetEntityNameRequestPacket packet, object? data)
    {
        if (data is not McApiNetPeer { ConnectionState: McApiConnectionState.Connected }) return;
        var entity = world.GetEntity(packet.Id);
        if (entity is null or VoiceCraftNetworkEntity { PositioningType: PositioningType.Client }) return;
        entity.Name = packet.Value;
    }

    private void HandleSetEntityMuteRequestPacket(McApiSetEntityMuteRequestPacket packet, object? data)
    {
        if (data is not McApiNetPeer { ConnectionState: McApiConnectionState.Connected }) return;
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

    private void HandleSetEntityDeafenRequestPacket(McApiSetEntityDeafenRequestPacket packet, object? data)
    {
        if (data is not McApiNetPeer { ConnectionState: McApiConnectionState.Connected }) return;
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

    private void HandleSetEntityTalkBitmaskRequestPacket(McApiSetEntityTalkBitmaskRequestPacket packet, object? data)
    {
        if (data is not McApiNetPeer { ConnectionState: McApiConnectionState.Connected }) return;
        var entity = world.GetEntity(packet.Id);
        if (entity == null) return;
        entity.TalkBitmask = packet.Value;
    }

    private void HandleSetEntityListenBitmaskRequestPacket(McApiSetEntityListenBitmaskRequestPacket packet,
        object? data)
    {
        if (data is not McApiNetPeer { ConnectionState: McApiConnectionState.Connected }) return;
        var entity = world.GetEntity(packet.Id);
        if (entity == null) return;
        entity.ListenBitmask = packet.Value;
    }

    private void HandleSetEntityEffectBitmaskRequestPacket(McApiSetEntityEffectBitmaskRequestPacket packet,
        object? data)
    {
        if (data is not McApiNetPeer { ConnectionState: McApiConnectionState.Connected }) return;
        var entity = world.GetEntity(packet.Id);
        if (entity == null) return;
        entity.EffectBitmask = packet.Value;
    }

    private void HandleSetEntityPositionRequestPacket(McApiSetEntityPositionRequestPacket packet, object? data)
    {
        if (data is not McApiNetPeer { ConnectionState: McApiConnectionState.Connected }) return;
        var entity = world.GetEntity(packet.Id);
        if (entity is null or VoiceCraftNetworkEntity { PositioningType: PositioningType.Client }) return;
        entity.Position = packet.Value;
    }

    private void HandleSetEntityRotationRequestPacket(McApiSetEntityRotationRequestPacket packet, object? data)
    {
        if (data is not McApiNetPeer { ConnectionState: McApiConnectionState.Connected }) return;
        var entity = world.GetEntity(packet.Id);
        if (entity is null or VoiceCraftNetworkEntity { PositioningType: PositioningType.Client }) return;
        entity.Rotation = packet.Value;
    }

    private void HandleSetEntityCaveFactorRequestPacket(McApiSetEntityCaveFactorRequestPacket packet, object? data)
    {
        if (data is not McApiNetPeer { ConnectionState: McApiConnectionState.Connected }) return;
        var entity = world.GetEntity(packet.Id);
        if (entity is null or VoiceCraftNetworkEntity { PositioningType: PositioningType.Client }) return;
        entity.CaveFactor = packet.Value;
    }

    private void HandleSetEntityMuffleFactorRequestPacket(McApiSetEntityMuffleFactorRequestPacket packet, object? data)
    {
        if (data is not McApiNetPeer { ConnectionState: McApiConnectionState.Connected }) return;
        var entity = world.GetEntity(packet.Id);
        if (entity is null or VoiceCraftNetworkEntity { PositioningType: PositioningType.Client }) return;
        entity.MuffleFactor = packet.Value;
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