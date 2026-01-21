using System;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Network.NetPeers;
using VoiceCraft.Network.Packets.McApiPackets;
using VoiceCraft.Network.Packets.McApiPackets.Request;
using VoiceCraft.Network.Packets.McApiPackets.Response;

namespace VoiceCraft.Network.Servers;

public abstract class McApiServer
{
    protected bool Disposed;

    public static Version Version { get; } = new(Constants.Major, Constants.Minor, Constants.Patch);
    public abstract string LoginToken { get; }
    public abstract uint MaxClients { get; }
    public abstract int ConnectedPeers { get; }

    public abstract void Start();
    public abstract void Update();
    public abstract void Stop();
    protected abstract void AcceptRequest(McApiLoginRequestPacket packet, object? data);
    protected abstract void RejectRequest(McApiLoginRequestPacket packet, string reason, object? data);
    protected abstract void Disconnect(McApiNetPeer netPeer, string reason);
    public abstract void SendPacket<T>(McApiNetPeer netPeer, T packet) where T : IMcApiPacket;
    public abstract void Broadcast<T>(T packet, params McApiNetPeer?[] excludes) where T : IMcApiPacket;

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
            case McApiPacketType.AcceptResponse:
                ProcessPacket<McApiAcceptResponsePacket>(reader, onParsed);
                break;
            case McApiPacketType.DenyResponse:
                ProcessPacket<McApiDenyResponsePacket>(reader, onParsed);
                break;
            case McApiPacketType.PingResponse:
                ProcessPacket<McApiPingResponsePacket>(reader, onParsed);
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
        }
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