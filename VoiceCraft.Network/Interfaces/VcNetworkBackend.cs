using System;
using System.Net;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Network.Packets.VcPackets;
using VoiceCraft.Network.Packets.VcPackets.Event;
using VoiceCraft.Network.Packets.VcPackets.Request;
using VoiceCraft.Network.Packets.VcPackets.Response;

namespace VoiceCraft.Network.Interfaces;

public abstract class VcNetworkBackend : IDisposable
{
    public abstract bool IsStarted { get; }
    public abstract int ConnectedPeersCount { get; }

    public abstract event Action<int>? OnStarted;
    public abstract event Action<string?>? OnStopped;
    public abstract event Action<VoiceCraftNetPeer>? OnLoginRequest;
    public abstract event Action<VoiceCraftNetPeer>? OnPeerConnected;
    public abstract event Action<VoiceCraftNetPeer>? OnPeerDisconnected;
    public event Action<VoiceCraftNetPeer, IVoiceCraftPacket>? OnNetworkReceive;
    public event Action<IPEndPoint, IVoiceCraftPacket>? OnNetworkReceiveUnconnected;

    public abstract void Start(int? port = null);

    public abstract void Connect(string ip, int port, Guid userGuid, Guid serverUserGuid, string locale,
        PositioningType positioningType);

    public abstract void Update();
    public abstract void Stop();

    public abstract void Reject(VoiceCraftNetPeer netPeer, string? reason = null);

    public abstract void SendUnconnectedPacket<T>(IPEndPoint endPoint, T packet) where T : IVoiceCraftPacket;
    
    public abstract void SendPacket<T>(VoiceCraftNetPeer netPeer, T packet,
        VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable) where T : IVoiceCraftPacket;

    public abstract void Broadcast<T>(T packet,
        VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable) where T : IVoiceCraftPacket;

    public abstract void Disconnect(VoiceCraftNetPeer netPeer, string? reason = null);
    public abstract void DisconnectAll(string? reason = null);
    public abstract void Dispose();

    protected virtual void ProcessPacket(VoiceCraftNetPeer netPeer, VcPacketType packetType, NetDataReader reader)
    {
        switch (packetType)
        {
            case VcPacketType.InfoRequest:
                ExecuteNetworkReceive<VcInfoRequestPacket>(netPeer, reader);
                break;
            case VcPacketType.LoginRequest:
                ExecuteNetworkReceive<VcLoginRequestPacket>(netPeer, reader);
                break;
            case VcPacketType.LogoutRequest:
                ExecuteNetworkReceive<VcLogoutRequestPacket>(netPeer, reader);
                break;
            case VcPacketType.InfoResponse:
                ExecuteNetworkReceive<VcInfoResponsePacket>(netPeer, reader);
                break;
            case VcPacketType.AcceptResponse:
                ExecuteNetworkReceive<VcAcceptResponsePacket>(netPeer, reader);
                break;
            case VcPacketType.DenyResponse:
                ExecuteNetworkReceive<VcDenyResponsePacket>(netPeer, reader);
                break;
            case VcPacketType.SetNameRequest:
                ExecuteNetworkReceive<VcSetNameRequestPacket>(netPeer, reader);
                break;
            case VcPacketType.AudioRequest:
                ExecuteNetworkReceive<VcAudioRequestPacket>(netPeer, reader);
                break;
            case VcPacketType.SetMuteRequest:
                ExecuteNetworkReceive<VcSetMuteRequestPacket>(netPeer, reader);
                break;
            case VcPacketType.SetDeafenRequest:
                ExecuteNetworkReceive<VcSetDeafenRequestPacket>(netPeer, reader);
                break;
            case VcPacketType.SetServerMuteRequest:
                ExecuteNetworkReceive<VcSetServerMuteRequestPacket>(netPeer, reader);
                break;
            case VcPacketType.SetServerDeafenRequest:
                ExecuteNetworkReceive<VcSetServerDeafenRequestPacket>(netPeer, reader);
                break;
            case VcPacketType.SetWorldIdRequest:
                ExecuteNetworkReceive<VcSetWorldIdRequestPacket>(netPeer, reader);
                break;
            case VcPacketType.SetTalkBitmaskRequest:
                ExecuteNetworkReceive<VcSetTalkBitmaskRequestPacket>(netPeer, reader);
                break;
            case VcPacketType.SetListenBitmaskRequest:
                ExecuteNetworkReceive<VcSetListenBitmaskRequestPacket>(netPeer, reader);
                break;
            case VcPacketType.SetEffectBitmaskRequest:
                ExecuteNetworkReceive<VcSetEffectBitmaskRequestPacket>(netPeer, reader);
                break;
            case VcPacketType.SetPositionRequest:
                ExecuteNetworkReceive<VcSetPositionRequestPacket>(netPeer, reader);
                break;
            case VcPacketType.SetRotationRequest:
                ExecuteNetworkReceive<VcSetRotationRequestPacket>(netPeer, reader);
                break;
            case VcPacketType.SetCaveFactorRequest:
                ExecuteNetworkReceive<VcSetCaveFactorRequest>(netPeer, reader);
                break;
            case VcPacketType.SetMuffleFactorRequest:
                ExecuteNetworkReceive<VcSetMuffleFactorRequest>(netPeer, reader);
                break;
            case VcPacketType.SetTitleRequest:
                ExecuteNetworkReceive<VcSetTitleRequestPacket>(netPeer, reader);
                break;
            case VcPacketType.SetDescriptionRequest:
                ExecuteNetworkReceive<VcSetDescriptionRequestPacket>(netPeer, reader);
                break;
            case VcPacketType.SetEntityVisibilityRequest:
                ExecuteNetworkReceive<VcSetEntityVisibilityRequestPacket>(netPeer, reader);
                break;
            case VcPacketType.OnEffectUpdated:
                ExecuteNetworkReceive<VcOnEffectUpdatedPacket>(netPeer, reader);
                break;
            case VcPacketType.OnEntityCreated:
                ExecuteNetworkReceive<VcOnEntityCreatedPacket>(netPeer, reader);
                break;
            case VcPacketType.OnNetworkEntityCreated:
                ExecuteNetworkReceive<VcOnNetworkEntityCreatedPacket>(netPeer, reader);
                break;
            case VcPacketType.OnEntityDestroyed:
                ExecuteNetworkReceive<VcOnEntityDestroyedPacket>(netPeer, reader);
                break;
            case VcPacketType.OnEntityNameUpdated:
                ExecuteNetworkReceive<VcOnEntityNameUpdatedPacket>(netPeer, reader);
                break;
            case VcPacketType.OnEntityMuteUpdated:
                ExecuteNetworkReceive<VcOnEntityMuteUpdatedPacket>(netPeer, reader);
                break;
            case VcPacketType.OnEntityDeafenUpdated:
                ExecuteNetworkReceive<VcOnEntityDeafenUpdatedPacket>(netPeer, reader);
                break;
            case VcPacketType.OnEntityServerMuteUpdated:
                ExecuteNetworkReceive<VcOnEntityServerMuteUpdatedPacket>(netPeer, reader);
                break;
            case VcPacketType.OnEntityServerDeafenUpdated:
                ExecuteNetworkReceive<VcOnEntityServerDeafenUpdatedPacket>(netPeer, reader);
                break;
            case VcPacketType.OnEntityTalkBitmaskUpdated:
                ExecuteNetworkReceive<VcOnEntityTalkBitmaskUpdatedPacket>(netPeer, reader);
                break;
            case VcPacketType.OnEntityListenBitmaskUpdated:
                ExecuteNetworkReceive<VcOnEntityListenBitmaskUpdatedPacket>(netPeer, reader);
                break;
            case VcPacketType.OnEntityEffectBitmaskUpdated:
                ExecuteNetworkReceive<VcOnEntityEffectBitmaskUpdatedPacket>(netPeer, reader);
                break;
            case VcPacketType.OnEntityPositionUpdated:
                ExecuteNetworkReceive<VcOnEntityPositionUpdatedPacket>(netPeer, reader);
                break;
            case VcPacketType.OnEntityRotationUpdated:
                ExecuteNetworkReceive<VcOnEntityRotationUpdatedPacket>(netPeer, reader);
                break;
            case VcPacketType.OnEntityCaveFactorUpdated:
                ExecuteNetworkReceive<VcOnEntityCaveFactorUpdatedPacket>(netPeer, reader);
                break;
            case VcPacketType.OnEntityMuffleFactorUpdated:
                ExecuteNetworkReceive<VcOnEntityMuffleFactorUpdatedPacket>(netPeer, reader);
                break;
            case VcPacketType.OnEntityAudioReceived:
                ExecuteNetworkReceive<VcOnEntityAudioReceivedPacket>(netPeer, reader);
                break;
            default:
                return;
        }
    }

    protected virtual void ProcessUnconnectedPacket(IPEndPoint endPoint, VcPacketType packetType, NetDataReader reader)
    {
        switch (packetType)
        {
            case VcPacketType.InfoRequest:
                ExecuteNetworkReceiveUnconnected<VcInfoRequestPacket>(endPoint, reader);
                break;
            case VcPacketType.LoginRequest:
            case VcPacketType.LogoutRequest:
                return;
            case VcPacketType.InfoResponse:
                ExecuteNetworkReceiveUnconnected<VcInfoResponsePacket>(endPoint, reader);
                break;
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

    private void ExecuteNetworkReceive<T>(VoiceCraftNetPeer netPeer, NetDataReader reader) where T : IVoiceCraftPacket
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

    private void ExecuteNetworkReceiveUnconnected<T>(IPEndPoint endPoint, NetDataReader reader)
        where T : IVoiceCraftPacket
    {
        var packet = PacketPool<T>.GetPacket();
        try
        {
            packet.Deserialize(reader);
            OnNetworkReceiveUnconnected?.Invoke(endPoint, packet);
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }
}