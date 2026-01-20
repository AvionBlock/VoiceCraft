using System;
using System.Linq;
using System.Net;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Core.World;
using VoiceCraft.Network.NetPeers;
using VoiceCraft.Network.Packets.VcPackets;
using VoiceCraft.Network.Packets.VcPackets.Request;
using VoiceCraft.Network.Packets.VcPackets.Response;
using VoiceCraft.Network.World;

namespace VoiceCraft.Network.Servers;

public abstract class VoiceCraftServer(VoiceCraftWorld world) : IDisposable
{
    protected bool Disposed;

    public static Version Version { get; } = new(Constants.Major, Constants.Minor, Constants.Patch);
    protected VoiceCraftWorld World { get; } = world;
    protected string Motd { get; set; } = "VoiceCraft Proximity Chat!";
    protected PositioningType PositioningType { get; set; } = PositioningType.Server;
    protected int ConnectedPeers => World.Entities.OfType<VoiceCraftNetworkEntity>().Count();
    protected uint MaxClients { get; set; }

    ~VoiceCraftServer()
    {
        Dispose(false);
    }

    public abstract void Start(int port);

    public abstract void Update();

    public abstract void Stop();

    public abstract void SendUnconnectedPacket<T>(IPEndPoint endPoint, T packet) where T : IVoiceCraftPacket;

    public abstract void SendPacket<T>(VoiceCraftNetPeer netPeer, T packet,
        VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable) where T : IVoiceCraftPacket;

    public abstract void Broadcast<T>(T packet, VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable,
        params VoiceCraftNetPeer?[] excludes) where T : IVoiceCraftPacket;

    protected abstract void AcceptRequest(VcLoginRequestPacket packet, object? data);
    protected abstract void RejectRequest(VcLoginRequestPacket packet, string reason, object? data);
    protected abstract void Disconnect(VoiceCraftNetPeer netPeer, string reason);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (Disposed) return;
        if (disposing) ;

        Disposed = true;
    }

    protected static void ProcessPacket(NetDataReader reader, Action<IVoiceCraftPacket> onParsed)
    {
        var packetType = (VcPacketType)reader.GetByte();
        switch (packetType)
        {
            case VcPacketType.LoginRequest:
                ProcessPacket<VcLoginRequestPacket>(reader, onParsed);
                break;
            case VcPacketType.SetNameRequest:
                ProcessPacket<VcSetNameRequestPacket>(reader, onParsed);
                break;
            case VcPacketType.AudioRequest:
                ProcessPacket<VcAudioRequestPacket>(reader, onParsed);
                break;
            case VcPacketType.SetMuteRequest:
                ProcessPacket<VcSetMuteRequestPacket>(reader, onParsed);
                break;
            case VcPacketType.SetDeafenRequest:
                ProcessPacket<VcSetDeafenRequestPacket>(reader, onParsed);
                break;
            case VcPacketType.SetWorldIdRequest:
                ProcessPacket<VcSetWorldIdRequestPacket>(reader, onParsed);
                break;
            case VcPacketType.SetPositionRequest:
                ProcessPacket<VcSetPositionRequestPacket>(reader, onParsed);
                break;
            case VcPacketType.SetRotationRequest:
                ProcessPacket<VcSetRotationRequestPacket>(reader, onParsed);
                break;
            case VcPacketType.SetCaveFactorRequest:
                ProcessPacket<VcSetCaveFactorRequest>(reader, onParsed);
                break;
            case VcPacketType.SetMuffleFactorRequest:
                ProcessPacket<VcSetMuffleFactorRequest>(reader, onParsed);
                break;
            case VcPacketType.InfoRequest:
            case VcPacketType.LogoutRequest:
            case VcPacketType.InfoResponse:
            case VcPacketType.AcceptResponse:
            case VcPacketType.DenyResponse:
            case VcPacketType.SetServerMuteRequest:
            case VcPacketType.SetServerDeafenRequest:
            case VcPacketType.SetTalkBitmaskRequest:
            case VcPacketType.SetListenBitmaskRequest:
            case VcPacketType.SetEffectBitmaskRequest:
            case VcPacketType.SetTitleRequest:
            case VcPacketType.SetDescriptionRequest:
            case VcPacketType.SetEntityVisibilityRequest:
            case VcPacketType.OnEffectUpdated:
            case VcPacketType.OnEntityCreated:
            case VcPacketType.OnNetworkEntityCreated:
            case VcPacketType.OnEntityDestroyed:
            case VcPacketType.OnEntityNameUpdated:
            case VcPacketType.OnEntityMuteUpdated:
            case VcPacketType.OnEntityDeafenUpdated:
            case VcPacketType.OnEntityServerMuteUpdated:
            case VcPacketType.OnEntityServerDeafenUpdated:
            case VcPacketType.OnEntityTalkBitmaskUpdated:
            case VcPacketType.OnEntityListenBitmaskUpdated:
            case VcPacketType.OnEntityEffectBitmaskUpdated:
            case VcPacketType.OnEntityPositionUpdated:
            case VcPacketType.OnEntityRotationUpdated:
            case VcPacketType.OnEntityCaveFactorUpdated:
            case VcPacketType.OnEntityMuffleFactorUpdated:
            case VcPacketType.OnEntityAudioReceived:
            default:
                return;
        }
    }

    protected static void ProcessUnconnectedPacket(NetDataReader reader, Action<IVoiceCraftPacket> onParsed)
    {
        var packetType = (VcPacketType)reader.GetByte();
        switch (packetType)
        {
            case VcPacketType.InfoRequest:
                ProcessPacket<VcInfoRequestPacket>(reader, onParsed);
                break;
            case VcPacketType.LoginRequest:
            case VcPacketType.LogoutRequest:
            case VcPacketType.InfoResponse:
            case VcPacketType.AcceptResponse:
            case VcPacketType.DenyResponse:
            case VcPacketType.SetNameRequest:
            case VcPacketType.AudioRequest:
            case VcPacketType.SetMuteRequest:
            case VcPacketType.SetDeafenRequest:
            case VcPacketType.SetServerMuteRequest:
            case VcPacketType.SetServerDeafenRequest:
            case VcPacketType.SetWorldIdRequest:
            case VcPacketType.SetTalkBitmaskRequest:
            case VcPacketType.SetListenBitmaskRequest:
            case VcPacketType.SetEffectBitmaskRequest:
            case VcPacketType.SetPositionRequest:
            case VcPacketType.SetRotationRequest:
            case VcPacketType.SetCaveFactorRequest:
            case VcPacketType.SetMuffleFactorRequest:
            case VcPacketType.SetTitleRequest:
            case VcPacketType.SetDescriptionRequest:
            case VcPacketType.SetEntityVisibilityRequest:
            case VcPacketType.OnEffectUpdated:
            case VcPacketType.OnEntityCreated:
            case VcPacketType.OnNetworkEntityCreated:
            case VcPacketType.OnEntityDestroyed:
            case VcPacketType.OnEntityNameUpdated:
            case VcPacketType.OnEntityMuteUpdated:
            case VcPacketType.OnEntityDeafenUpdated:
            case VcPacketType.OnEntityServerMuteUpdated:
            case VcPacketType.OnEntityServerDeafenUpdated:
            case VcPacketType.OnEntityTalkBitmaskUpdated:
            case VcPacketType.OnEntityListenBitmaskUpdated:
            case VcPacketType.OnEntityEffectBitmaskUpdated:
            case VcPacketType.OnEntityPositionUpdated:
            case VcPacketType.OnEntityRotationUpdated:
            case VcPacketType.OnEntityCaveFactorUpdated:
            case VcPacketType.OnEntityMuffleFactorUpdated:
            case VcPacketType.OnEntityAudioReceived:
            default:
                return;
        }
    }

    protected void ExecutePacket(IVoiceCraftPacket packet, object? data = null)
    {
        switch (packet)
        {
            //Core. DO NOT CHANGE
            //Requests
            case VcInfoRequestPacket infoRequestPacket:
                HandleInfoRequestPacket(infoRequestPacket, data);
                break;
            case VcLoginRequestPacket loginRequestPacket:
                HandleLoginRequestPacket(loginRequestPacket, data);
                break;
            case VcSetNameRequestPacket setNameRequestPacket:
                HandleSetNameRequestPacket(setNameRequestPacket, data);
                break;
            case VcAudioRequestPacket audioRequestPacket:
                HandleAudioRequestPacket(audioRequestPacket, data);
                break;
            case VcSetMuteRequestPacket setMuteRequestPacket:
                HandleSetMuteRequestPacket(setMuteRequestPacket, data);
                break;
            case VcSetDeafenRequestPacket setDeafenRequestPacket:
                HandleSetDeafenRequestPacket(setDeafenRequestPacket, data);
                break;
            case VcSetWorldIdRequestPacket setWorldIdRequestPacket:
                HandleSetWorldIdRequestPacket(setWorldIdRequestPacket, data);
                break;
            case VcSetPositionRequestPacket setPositionRequestPacket:
                HandleSetPositionRequestPacket(setPositionRequestPacket, data);
                break;
            case VcSetRotationRequestPacket setRotationRequestPacket:
                HandleSetRotationRequestPacket(setRotationRequestPacket, data);
                break;
            case VcSetCaveFactorRequest setCaveFactorRequestPacket:
                HandleSetCaveFactorRequestPacket(setCaveFactorRequestPacket, data);
                break;
            case VcSetMuffleFactorRequest setMuffleFactorRequestPacket:
                HandleSetMuffleFactorRequestPacket(setMuffleFactorRequestPacket, data);
                break;
            default:
                return;
        }
    }

    private void HandleInfoRequestPacket(VcInfoRequestPacket packet, object? data)
    {
        if (data is not IPEndPoint endPoint) return;
        var responsePacket = PacketPool<VcInfoResponsePacket>.GetPacket()
            .Set(Motd, ConnectedPeers, PositioningType, packet.Tick, Version);
        SendUnconnectedPacket(endPoint, responsePacket);
    }

    private void HandleLoginRequestPacket(VcLoginRequestPacket packet, object? data)
    {
        if (packet.Version.Major != Version.Major || packet.Version.Minor != Version.Minor)
        {
            RejectRequest(packet, "VoiceCraft.DisconnectReason.IncompatibleVersion", data);
            return;
        }
        
        if (ConnectedPeers >= MaxClients)
        {
            RejectRequest(packet, "VoiceCraft.DisconnectReason.ServerFull", data);
            return;
        }

        if (packet.PositioningType != PositioningType)
            switch (PositioningType)
            {
                case PositioningType.Server:
                    RejectRequest(packet, "VoiceCraft.DisconnectReason.ServerSidedOnly", data);
                    return;
                case PositioningType.Client:
                    RejectRequest(packet, "VoiceCraft.DisconnectReason.ClientSidedOnly", data);
                    return;
                default:
                    RejectRequest(packet, "VoiceCraft.DisconnectReason.Error", data);
                    return;
            }
        
        AcceptRequest(packet, data);
    }

    private static void HandleSetNameRequestPacket(VcSetNameRequestPacket packet, object? data)
    {
        if (data is not VoiceCraftNetworkEntity networkEntity) return;
        networkEntity.Name = packet.Value;
    }

    private static void HandleAudioRequestPacket(VcAudioRequestPacket packet, object? data)
    {
        if (data is not VoiceCraftNetworkEntity networkEntity || networkEntity.Muted ||
            networkEntity.ServerMuted) return;
        networkEntity.ReceiveAudio(packet.Buffer, packet.Timestamp, packet.FrameLoudness);
    }

    private static void HandleSetMuteRequestPacket(VcSetMuteRequestPacket packet, object? data)
    {
        if (data is not VoiceCraftNetworkEntity networkEntity) return;
        networkEntity.Muted = packet.Value;
    }

    private static void HandleSetDeafenRequestPacket(VcSetDeafenRequestPacket packet, object? data)
    {
        if (data is not VoiceCraftNetworkEntity networkEntity) return;
        networkEntity.Deafened = packet.Value;
    }

    private static void HandleSetWorldIdRequestPacket(VcSetWorldIdRequestPacket packet, object? data)
    {
        if (data is not VoiceCraftNetworkEntity
            {
                PositioningType: PositioningType.Client
            } networkEntity) return;
        networkEntity.WorldId = packet.Value;
    }

    private static void HandleSetPositionRequestPacket(VcSetPositionRequestPacket packet, object? data)
    {
        if (data is not VoiceCraftNetworkEntity
            {
                PositioningType: PositioningType.Client
            } networkEntity) return;
        networkEntity.Position = packet.Value;
    }

    private static void HandleSetRotationRequestPacket(VcSetRotationRequestPacket packet, object? data)
    {
        if (data is not VoiceCraftNetworkEntity
            {
                PositioningType: PositioningType.Client
            } networkEntity) return;
        networkEntity.Rotation = packet.Value;
    }

    private static void HandleSetCaveFactorRequestPacket(VcSetCaveFactorRequest packet, object? data)
    {
        if (data is not VoiceCraftNetworkEntity
            {
                PositioningType: PositioningType.Client
            } networkEntity) return;
        networkEntity.CaveFactor = packet.Value;
    }

    private static void HandleSetMuffleFactorRequestPacket(VcSetMuffleFactorRequest packet, object? data)
    {
        if (data is not VoiceCraftNetworkEntity
            {
                PositioningType: PositioningType.Client
            } networkEntity) return;
        networkEntity.MuffleFactor = packet.Value;
    }

    private static void ProcessPacket<T>(NetDataReader reader, Action<IVoiceCraftPacket> onParsed)
        where T : IVoiceCraftPacket
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