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
                ProcessPacket<McApiLoginRequestPacket>(reader, onParsed);
                break;
            case McApiPacketType.LogoutRequest:
                ProcessPacket<McApiLogoutRequestPacket>(reader, onParsed);
                break;
            case McApiPacketType.PingRequest:
                ProcessPacket<McApiPingRequestPacket>(reader, onParsed);
                break;
            case McApiPacketType.ResetRequest:
                ProcessPacket<McApiResetRequestPacket>(reader, onParsed);
                break;
            case McApiPacketType.SetEffectRequest:
                ProcessPacket<McApiSetEffectRequestPacket>(reader, onParsed);
                break;
            case McApiPacketType.ClearEffectsRequest:
                ProcessPacket<McApiClearEffectsRequestPacket>(reader, onParsed);
                break;
            case McApiPacketType.CreateEntityRequest:
                ProcessPacket<McApiCreateEntityRequestPacket>(reader, onParsed);
                break;
            case McApiPacketType.DestroyEntityRequest:
                ProcessPacket<McApiDestroyEntityRequestPacket>(reader, onParsed);
                break;
            case McApiPacketType.EntityAudioRequest:
                ProcessPacket<McApiEntityAudioRequestPacket>(reader, onParsed);
                break;
            case McApiPacketType.SetEntityTitleRequest:
                ProcessPacket<McApiSetEntityTitleRequestPacket>(reader, onParsed);
                break;
            case McApiPacketType.SetEntityDescriptionRequest:
                ProcessPacket<McApiSetEntityDescriptionRequestPacket>(reader, onParsed);
                break;
            case McApiPacketType.SetEntityWorldIdRequest:
                ProcessPacket<McApiSetEntityWorldIdRequestPacket>(reader, onParsed);
                break;
            case McApiPacketType.SetEntityNameRequest:
                ProcessPacket<McApiSetEntityNameRequestPacket>(reader, onParsed);
                break;
            case McApiPacketType.SetEntityMuteRequest:
                ProcessPacket<McApiSetEntityMuteRequestPacket>(reader, onParsed);
                break;
            case McApiPacketType.SetEntityDeafenRequest:
                ProcessPacket<McApiSetEntityDeafenRequestPacket>(reader, onParsed);
                break;
            case McApiPacketType.SetEntityTalkBitmaskRequest:
                ProcessPacket<McApiSetEntityTalkBitmaskRequestPacket>(reader, onParsed);
                break;
            case McApiPacketType.SetEntityListenBitmaskRequest:
                ProcessPacket<McApiSetEntityListenBitmaskRequestPacket>(reader, onParsed);
                break;
            case McApiPacketType.SetEntityEffectBitmaskRequest:
                ProcessPacket<McApiSetEntityEffectBitmaskRequestPacket>(reader, onParsed);
                break;
            case McApiPacketType.SetEntityPositionRequest:
                ProcessPacket<McApiSetEntityPositionRequestPacket>(reader, onParsed);
                break;
            case McApiPacketType.SetEntityRotationRequest:
                ProcessPacket<McApiSetEntityRotationRequestPacket>(reader, onParsed);
                break;
            case McApiPacketType.SetEntityCaveFactorRequest:
                ProcessPacket<McApiSetEntityCaveFactorRequestPacket>(reader, onParsed);
                break;
            case McApiPacketType.SetEntityMuffleFactorRequest:
                ProcessPacket<McApiSetEntityMuffleFactorRequestPacket>(reader, onParsed);
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
            SendPacket(netPeer,
                PacketPool<McApiAcceptResponsePacket>.GetPacket().Set(packet.RequestId, netPeer.SessionToken));
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
        SendPacket(netPeer, PacketPool<McApiPingResponsePacket>.GetPacket().Set());
    }

    private void HandleResetRequestPacket(McApiResetRequestPacket packet, object? data)
    {
        if (data is not McApiNetPeer { ConnectionState: McApiConnectionState.Connected } netPeer) return;
        try
        {
            world.Reset();
            audioEffectSystem.Reset();
            SendPacket(netPeer, PacketPool<McApiResetResponsePacket>.GetPacket().Set(packet.RequestId));
        }
        catch
        {
            SendPacket(netPeer, PacketPool<McApiResetResponsePacket>.GetPacket()
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
            SendPacket(netPeer, PacketPool<McApiCreateEntityResponsePacket>.GetPacket()
                .Set(packet.RequestId, McApiCreateEntityResponsePacket.ResponseCodes.Ok, entity.Id));
        }
        catch
        {
            SendPacket(netPeer,
                PacketPool<McApiCreateEntityResponsePacket>.GetPacket().Set(packet.RequestId,
                    McApiCreateEntityResponsePacket.ResponseCodes.Failure));
        }
    }

    private void HandleDestroyEntityRequestPacket(McApiDestroyEntityRequestPacket packet, object? data)
    {
        if (data is not McApiNetPeer { ConnectionState: McApiConnectionState.Connected } netPeer) return;
        try
        {
            world.DestroyEntity(packet.Id);
            SendPacket(netPeer, PacketPool<McApiDestroyEntityResponsePacket>.GetPacket()
                .Set(packet.RequestId));
        }
        catch
        {
            SendPacket(netPeer, PacketPool<McApiDestroyEntityResponsePacket>.GetPacket()
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

    private static void ProcessPacket<T>(NetDataReader reader, Action<IMcApiPacket> onParsed)
        where T : IMcApiPacket
    {
        var packet = PacketPool<T>.GetPacket();
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