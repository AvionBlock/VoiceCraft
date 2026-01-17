using System;
using System.Net;
using System.Threading.Tasks;
using VoiceCraft.Core;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Interfaces;
using VoiceCraft.Network.NetPeers;
using VoiceCraft.Network.Packets.VcPackets;
using VoiceCraft.Network.Packets.VcPackets.Request;
using VoiceCraft.Network.Packets.VcPackets.Response;
using VoiceCraft.Network.World;

namespace VoiceCraft.Network;

public class VoiceCraftServer : IDisposable
{
    public static readonly Version Version = new(Constants.Major, Constants.Minor, Constants.Patch);
    private readonly VcNetworkBackend _networkBackend;
    private readonly VoiceCraftWorld _world;
    private bool _disposed;

    public uint MaxClients { get; set; }
    public string Motd { get; set; } = "VoiceCraft Proximity Chat!";
    public PositioningType PositioningType { get; private set; } = PositioningType.Server;

    public VoiceCraftServer(VcNetworkBackend networkBackend, VoiceCraftWorld world)
    {
        _networkBackend = networkBackend;
        _world = world;
        _networkBackend.OnLoginRequest += NetworkBackendOnLoginRequest;
        _networkBackend.OnNetworkReceive += NetworkBackendOnNetworkReceive;
        _networkBackend.OnNetworkReceiveUnconnected += NetworkBackendOnNetworkReceiveUnconnected;
    }

    ~VoiceCraftServer()
    {
        Dispose(false);
    }

    public async Task StartAsync(int port, uint maxClients, string motd, PositioningType positioningType)
    {
        await StopAsync();
        MaxClients = maxClients;
        Motd = motd;
        PositioningType = positioningType;
        await Task.Run(() => _networkBackend.Start(port));
    }

    public void Update()
    {
        if (!_networkBackend.IsStarted) return;
        _networkBackend.Update();
    }

    public async Task StopAsync(string? reason = null)
    {
        if (!_networkBackend.IsStarted) return;
        await Task.Run(() =>
        {
            _networkBackend.DisconnectAll(reason);
            _networkBackend.Stop();
        });
    }

    public void SendPacket<T>(VoiceCraftNetPeer netPeer, T packet,
        VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable) where T : IVoiceCraftPacket
    {
        if (!_networkBackend.IsStarted) return;
        try
        {
            _networkBackend.SendPacket(netPeer, packet, deliveryMethod);
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    public void Broadcast<T>(T packet,
        VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable) where T : IVoiceCraftPacket
    {
        if (!_networkBackend.IsStarted) return;
        try
        {
            _networkBackend.Broadcast(packet, deliveryMethod);
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    public void Disconnect(VoiceCraftNetPeer netPeer, string? reason = null)
    {
        _networkBackend.Disconnect(netPeer, reason);
    }

    public void DisconnectAll(string? reason = null)
    {
        _networkBackend.DisconnectAll(reason);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    //Event Handling
    private void NetworkBackendOnLoginRequest(VoiceCraftNetPeer netPeer)
    {
        try
        {
            if (netPeer.Version.Major != Version.Major || netPeer.Version.Minor != Version.Minor)
            {
                _networkBackend.Reject(netPeer, "VoiceCraft.DisconnectReason.IncompatibleVersion");
                return;
            }

            if (_networkBackend.ConnectedPeersCount >= MaxClients)
            {
                _networkBackend.Reject(netPeer, "VoiceCraft.DisconnectReason.ServerFull");
                return;
            }

            if (netPeer.PositioningType != PositioningType)
                switch (PositioningType)
                {
                    case PositioningType.Server:
                        _networkBackend.Reject(netPeer, "VoiceCraft.DisconnectReason.ServerSidedOnly");
                        return;
                    case PositioningType.Client:
                        _networkBackend.Reject(netPeer, "VoiceCraft.DisconnectReason.ClientSidedOnly");
                        return;
                    default:
                        _networkBackend.Reject(netPeer, "VoiceCraft.DisconnectReason.Error");
                        return;
                }

            var id = _world.GetNextId();
            var entity = new VoiceCraftNetworkEntity(netPeer, id, _world);
            _world.AddEntity(entity);
            netPeer.Accept();
        }
        catch
        {
            if (netPeer.ConnectionState == VcConnectionState.LoginRequested)
            {
                _networkBackend.Reject(netPeer, "VoiceCraft.DisconnectReason.Error");
                return;
            }

            _networkBackend.Disconnect(netPeer, "VoiceCraft.DisconnectReason.Error");
            throw; //This will go up the stack until it reaches a logger.
        }
    }

    private static void NetworkBackendOnNetworkReceive(VoiceCraftNetPeer netPeer, IVoiceCraftPacket packet)
    {
        switch (packet)
        {
            case VcSetNameRequestPacket setNameRequestPacket:
                HandleSetNameRequestPacket(netPeer, setNameRequestPacket);
                break;
            case VcAudioRequestPacket audioRequestPacket:
                HandleAudioRequestPacket(netPeer, audioRequestPacket);
                break;
            case VcSetMuteRequestPacket setMuteRequestPacket:
                HandleSetMuteRequestPacket(netPeer, setMuteRequestPacket);
                break;
            case VcSetDeafenRequestPacket setDeafenRequestPacket:
                HandleSetDeafenRequestPacket(netPeer, setDeafenRequestPacket);
                break;
            case VcSetWorldIdRequestPacket setWorldIdRequestPacket:
                HandleSetWorldIdRequestPacket(netPeer, setWorldIdRequestPacket);
                break;
            case VcSetPositionRequestPacket setPositionRequestPacket:
                HandleSetPositionRequestPacket(netPeer, setPositionRequestPacket);
                break;
            case VcSetRotationRequestPacket setRotationRequestPacket:
                HandleSetRotationRequestPacket(netPeer, setRotationRequestPacket);
                break;
            case VcSetCaveFactorRequest setCaveFactorRequestPacket:
                HandleSetCaveFactorRequestPacket(netPeer, setCaveFactorRequestPacket);
                break;
            case VcSetMuffleFactorRequest setMuffleFactorRequestPacket:
                HandleSetMuffleFactorRequestPacket(netPeer, setMuffleFactorRequestPacket);
                break;
        }
    }

    private void NetworkBackendOnNetworkReceiveUnconnected(IPEndPoint endPoint, IVoiceCraftPacket packet)
    {
        switch (packet)
        {
            case VcInfoRequestPacket infoRequestPacket:
                HandleInfoRequestPacket(endPoint, infoRequestPacket);
                break;
        }
    }

    //Handling
    private void HandleInfoRequestPacket(IPEndPoint endPoint, VcInfoRequestPacket packet)
    {
        _networkBackend.SendUnconnectedPacket(endPoint,
            PacketPool<VcInfoResponsePacket>.GetPacket().Set(Motd, _networkBackend.ConnectedPeersCount,
                PositioningType, packet.Tick, Version));
    }

    private static void HandleSetNameRequestPacket(VoiceCraftNetPeer netPeer, VcSetNameRequestPacket packet)
    {
        if (netPeer.Tag is not VoiceCraftNetworkEntity networkEntity) return;
        networkEntity.Name = packet.Value;
    }

    private static void HandleAudioRequestPacket(VoiceCraftNetPeer netPeer, VcAudioRequestPacket packet)
    {
        if (netPeer.Tag is not VoiceCraftNetworkEntity networkEntity || networkEntity.Muted ||
            networkEntity.ServerMuted) return;
        networkEntity.ReceiveAudio(packet.Data, packet.Timestamp, packet.FrameLoudness);
    }

    private static void HandleSetMuteRequestPacket(VoiceCraftNetPeer netPeer, VcSetMuteRequestPacket packet)
    {
        if (netPeer.Tag is not VoiceCraftNetworkEntity networkEntity) return;
        networkEntity.Muted = packet.Value;
    }

    private static void HandleSetDeafenRequestPacket(VoiceCraftNetPeer netPeer, VcSetDeafenRequestPacket packet)
    {
        if (netPeer.Tag is not VoiceCraftNetworkEntity networkEntity) return;
        networkEntity.Deafened = packet.Value;
    }

    private static void HandleSetWorldIdRequestPacket(VoiceCraftNetPeer netPeer, VcSetWorldIdRequestPacket packet)
    {
        if (netPeer.Tag is not VoiceCraftNetworkEntity
            {
                PositioningType: PositioningType.Client
            } networkEntity) return;
        networkEntity.WorldId = packet.Value;
    }

    private static void HandleSetPositionRequestPacket(VoiceCraftNetPeer netPeer, VcSetPositionRequestPacket packet)
    {
        if (netPeer.Tag is not VoiceCraftNetworkEntity
            {
                PositioningType: PositioningType.Client
            } networkEntity) return;
        networkEntity.Position = packet.Value;
    }

    private static void HandleSetRotationRequestPacket(VoiceCraftNetPeer netPeer, VcSetRotationRequestPacket packet)
    {
        if (netPeer.Tag is not VoiceCraftNetworkEntity
            {
                PositioningType: PositioningType.Client
            } networkEntity) return;
        networkEntity.Rotation = packet.Value;
    }

    private static void HandleSetCaveFactorRequestPacket(VoiceCraftNetPeer netPeer, VcSetCaveFactorRequest packet)
    {
        if (netPeer.Tag is not VoiceCraftNetworkEntity
            {
                PositioningType: PositioningType.Client
            } networkEntity) return;
        networkEntity.CaveFactor = packet.Value;
    }

    private static void HandleSetMuffleFactorRequestPacket(VoiceCraftNetPeer netPeer, VcSetMuffleFactorRequest packet)
    {
        if (netPeer.Tag is not VoiceCraftNetworkEntity
            {
                PositioningType: PositioningType.Client
            } networkEntity) return;
        networkEntity.MuffleFactor = packet.Value;
    }

    //Private Internal Handling
    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            _networkBackend.Dispose();

            _networkBackend.OnLoginRequest -= NetworkBackendOnLoginRequest;
            _networkBackend.OnNetworkReceive -= NetworkBackendOnNetworkReceive;
            _networkBackend.OnNetworkReceiveUnconnected -= NetworkBackendOnNetworkReceiveUnconnected;
        }

        _disposed = true;
    }
}