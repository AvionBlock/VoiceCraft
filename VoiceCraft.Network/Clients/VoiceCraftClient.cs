using System;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Packets.VcPackets;
using VoiceCraft.Network.Packets.VcPackets.Event;
using VoiceCraft.Network.Packets.VcPackets.Request;
using VoiceCraft.Network.Packets.VcPackets.Response;

namespace VoiceCraft.Network.Clients;

public abstract class VoiceCraftClient() : VoiceCraftEntity(0, new VoiceCraftWorld()), IDisposable
{
    protected bool Disposed;
    
    public static Version Version { get; } = new(Constants.Major, Constants.Minor, Constants.Patch);
    public VcConnectionState ConnectionState { get; protected set; }
    public abstract event Action<string?>? OnDisconnected;

    ~VoiceCraftClient()
    {
        Dispose(false);
    }

    public abstract Task<ServerInfo> PingAsync(string ip, int port, CancellationToken token = default);
    
    public abstract Task ConnectAsync(string ip, int port, Guid userGuid, Guid serverUserGuid, string locale,
        PositioningType positioningType);

    public abstract void Update();
    public abstract Task DisconnectAsync(string? reason = null);
    
    public abstract void SendUnconnectedPacket<T>(string ip, int port, T packet) where T : IVoiceCraftPacket;

    public abstract void SendPacket<T>(T packet,
        VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable) where T : IVoiceCraftPacket;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected static IVoiceCraftPacket? ProcessPacket(NetDataReader reader)
    {
        try
        {
            var packetType = (VcPacketType)reader.GetByte();
            IVoiceCraftPacket? packet;
            switch (packetType)
            {
                case VcPacketType.LogoutRequest:
                    packet = ProcessPacket<VcLogoutRequestPacket>(reader);
                    break;
                case VcPacketType.InfoResponse:
                    packet = ProcessPacket<VcInfoResponsePacket>(reader);
                    break;
                case VcPacketType.AcceptResponse:
                    packet = ProcessPacket<VcAcceptResponsePacket>(reader);
                    break;
                case VcPacketType.DenyResponse:
                    packet = ProcessPacket<VcDenyResponsePacket>(reader);
                    break;
                case VcPacketType.SetNameRequest:
                    packet = ProcessPacket<VcSetNameRequestPacket>(reader);
                    break;
                case VcPacketType.SetServerMuteRequest:
                    packet = ProcessPacket<VcSetServerMuteRequestPacket>(reader);
                    break;
                case VcPacketType.SetServerDeafenRequest:
                    packet = ProcessPacket<VcSetServerDeafenRequestPacket>(reader);
                    break;
                case VcPacketType.SetWorldIdRequest:
                    packet = ProcessPacket<VcSetWorldIdRequestPacket>(reader);
                    break;
                case VcPacketType.SetTalkBitmaskRequest:
                    packet = ProcessPacket<VcSetTalkBitmaskRequestPacket>(reader);
                    break;
                case VcPacketType.SetListenBitmaskRequest:
                    packet = ProcessPacket<VcSetListenBitmaskRequestPacket>(reader);
                    break;
                case VcPacketType.SetEffectBitmaskRequest:
                    packet = ProcessPacket<VcSetEffectBitmaskRequestPacket>(reader);
                    break;
                case VcPacketType.SetPositionRequest:
                    packet = ProcessPacket<VcSetPositionRequestPacket>(reader);
                    break;
                case VcPacketType.SetRotationRequest:
                    packet = ProcessPacket<VcSetRotationRequestPacket>(reader);
                    break;
                case VcPacketType.SetCaveFactorRequest:
                    packet = ProcessPacket<VcSetCaveFactorRequest>(reader);
                    break;
                case VcPacketType.SetMuffleFactorRequest:
                    packet = ProcessPacket<VcSetMuffleFactorRequest>(reader);
                    break;
                case VcPacketType.SetTitleRequest:
                    packet = ProcessPacket<VcSetTitleRequestPacket>(reader);
                    break;
                case VcPacketType.SetDescriptionRequest:
                    packet = ProcessPacket<VcSetDescriptionRequestPacket>(reader);
                    break;
                case VcPacketType.SetEntityVisibilityRequest:
                    packet = ProcessPacket<VcSetEntityVisibilityRequestPacket>(reader);
                    break;
                case VcPacketType.OnEffectUpdated:
                    packet = ProcessPacket<VcOnEffectUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityCreated:
                    packet = ProcessPacket<VcOnEntityCreatedPacket>(reader);
                    break;
                case VcPacketType.OnNetworkEntityCreated:
                    packet = ProcessPacket<VcOnNetworkEntityCreatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityDestroyed:
                    packet = ProcessPacket<VcOnEntityDestroyedPacket>(reader);
                    break;
                case VcPacketType.OnEntityNameUpdated:
                    packet = ProcessPacket<VcOnEntityNameUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityMuteUpdated:
                    packet = ProcessPacket<VcOnEntityMuteUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityDeafenUpdated:
                    packet = ProcessPacket<VcOnEntityDeafenUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityServerMuteUpdated:
                    packet = ProcessPacket<VcOnEntityServerMuteUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityServerDeafenUpdated:
                    packet = ProcessPacket<VcOnEntityServerDeafenUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityTalkBitmaskUpdated:
                    packet = ProcessPacket<VcOnEntityTalkBitmaskUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityListenBitmaskUpdated:
                    packet = ProcessPacket<VcOnEntityListenBitmaskUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityEffectBitmaskUpdated:
                    packet = ProcessPacket<VcOnEntityEffectBitmaskUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityPositionUpdated:
                    packet = ProcessPacket<VcOnEntityPositionUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityRotationUpdated:
                    packet = ProcessPacket<VcOnEntityRotationUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityCaveFactorUpdated:
                    packet = ProcessPacket<VcOnEntityCaveFactorUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityMuffleFactorUpdated:
                    packet = ProcessPacket<VcOnEntityMuffleFactorUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityAudioReceived:
                    packet = ProcessPacket<VcOnEntityAudioReceivedPacket>(reader);
                    break;
                case VcPacketType.InfoRequest:
                case VcPacketType.LoginRequest:
                case VcPacketType.AudioRequest:
                case VcPacketType.SetMuteRequest:
                case VcPacketType.SetDeafenRequest:
                default:
                    return null;
            }

            return packet;
        }
        catch
        {
            return null;
        }
    }

    protected static IVoiceCraftPacket? ProcessUnconnectedPacket(NetDataReader reader)
    {
        try
        {
            var packetType = (VcPacketType)reader.GetByte();
            IVoiceCraftPacket? packet;
            switch (packetType)
            {
                case VcPacketType.InfoRequest:
                    packet = ProcessPacket<VcInfoRequestPacket>(reader);
                    break;
                case VcPacketType.LoginRequest:
                case VcPacketType.LogoutRequest:
                    return null;
                case VcPacketType.InfoResponse:
                    packet = ProcessPacket<VcInfoResponsePacket>(reader);
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
                    return null;
            }

            return packet;
        }
        catch
        {
            return null;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (Disposed) return;
        if (disposing)
        {
            World.Dispose();
            Destroy();
        }
        Disposed = true;
    }

    private static IVoiceCraftPacket ProcessPacket<T>(NetDataReader reader) where T : IVoiceCraftPacket
    {
        var packet = PacketPool<T>.GetPacket();
        packet.Deserialize(reader);
        return packet;
    }
}