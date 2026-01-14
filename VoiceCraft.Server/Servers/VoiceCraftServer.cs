using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using Spectre.Console;
using VoiceCraft.Core;
using VoiceCraft.Core.Locales;
using VoiceCraft.Core.Network.VcPackets;
using VoiceCraft.Core.Network.VcPackets.Request;
using VoiceCraft.Core.Network.VcPackets.Response;
using VoiceCraft.Core.World;
using VoiceCraft.Server.Config;
using VoiceCraft.Server.Systems;

namespace VoiceCraft.Server.Servers;

public class VoiceCraftServer : IDisposable, INetEventListener
{
    public static readonly Version Version = new(Constants.Major, Constants.Minor, Constants.Patch);

    //Systems
    private readonly AudioEffectSystem _audioEffectSystem;

    //Networking
    private readonly NetDataWriter _dataWriter = new();
    private readonly NetManager _netManager;
    private bool _isDisposed;

    public VoiceCraftServer(AudioEffectSystem audioEffectSystem, VoiceCraftWorld world)
    {
        _netManager = new NetManager(this)
        {
            AutoRecycle = true,
            UnconnectedMessagesEnabled = true
        };

        _audioEffectSystem = audioEffectSystem;
        World = world;
    }

    //Public Properties
    public VoiceCraftConfig Config { get; private set; } = new();
    public VoiceCraftWorld World { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    //Network Handling
    public void OnPeerConnected(NetPeer peer)
    {
        //Do nothing.
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info)
    {
        if (peer.Tag is not VoiceCraftNetworkEntity networkEntity || networkEntity.Destroyed) return;
        World.DestroyEntity(networkEntity.Id);
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        //Do nothing.
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber,
        DeliveryMethod deliveryMethod)
    {
        try
        {
            var packetType = reader.GetByte();
            var pt = (VcPacketType)packetType;
            ProcessPacket(pt, reader, peer);
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
        }
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader,
        UnconnectedMessageType messageType)
    {
        try
        {
            var packetType = reader.GetByte();
            var pt = (VcPacketType)packetType;
            ProcessUnconnectedPacket(pt, reader, remoteEndPoint);
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
        }
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        //Do nothing
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        if (_netManager.ConnectedPeersCount >= Config.MaxClients)
        {
            RejectRequest(request, "VoiceCraft.DisconnectReason.ServerFull");
            return;
        }

        if (request.Data.IsNull)
        {
            RejectRequest(request, "VoiceCraft.DisconnectReason.Kicked");
            return;
        }

        try
        {
            var loginPacket = PacketPool<VcLoginRequestPacket>.GetPacket();
            loginPacket.Deserialize(request.Data);
            HandleLoginRequestPacket(loginPacket, request);
        }
        catch (Exception ex)
        {
            RejectRequest(request, "VoiceCraft.DisconnectReason.Error");
            LogService.Log(ex);
        }
    }

    ~VoiceCraftServer()
    {
        Dispose(false);
    }

    public void Start(VoiceCraftConfig? config = null)
    {
        Stop();

        AnsiConsole.WriteLine(Localizer.Get("VoiceCraftServer.Starting"));
        if (config != null)
            Config = config;

        if (_netManager.IsRunning || _netManager.Start((int)Config.Port))
            AnsiConsole.MarkupLine($"[green]{Localizer.Get("VoiceCraftServer.Success")}[/]");
        else
            throw new Exception(Localizer.Get("VoiceCraftServer.Exceptions.Failed"));
    }

    public void Update()
    {
        _netManager.PollEvents();
    }

    public void Stop()
    {
        if (!_netManager.IsRunning) return;
        AnsiConsole.WriteLine(Localizer.Get("VoiceCraftServer.Stopping"));
        DisconnectAll("VoiceCraft.DisconnectReason.Shutdown");
        _netManager.Stop();
        AnsiConsole.WriteLine(Localizer.Get("VoiceCraftServer.Stopped"));
    }

    public void RejectRequest(ConnectionRequest request, string? reason = null)
    {
        if (reason == null)
        {
            request.Reject();
            return;
        }

        var packet = PacketPool<VcDenyResponsePacket>.GetPacket().Set(reason: reason);
        try
        {
            lock (_dataWriter)
            {
                _dataWriter.Reset();
                _dataWriter.Put((byte)packet.PacketType);
                packet.Serialize(_dataWriter);
                request.Reject(_dataWriter);
            }
        }
        finally
        {
            PacketPool<VcDenyResponsePacket>.Return(packet);
        }
    }

    public void DisconnectPeer(NetPeer peer, string? reason = null)
    {
        if (reason == null)
        {
            peer.Disconnect();
            return;
        }

        var packet = PacketPool<VcLogoutRequestPacket>.GetPacket().Set(reason);
        try
        {
            lock (_dataWriter)
            {
                _dataWriter.Reset();
                _dataWriter.Put((byte)packet.PacketType);
                packet.Serialize(_dataWriter);
                peer.Disconnect(_dataWriter);
            }
        }
        finally
        {
            PacketPool<VcLogoutRequestPacket>.Return(packet);
        }
    }

    public void DisconnectAll(string? reason = null)
    {
        if (reason == null)
        {
            _netManager.DisconnectAll();
            return;
        }

        var packet = PacketPool<VcLogoutRequestPacket>.GetPacket().Set(reason);
        try
        {
            lock (_dataWriter)
            {
                _dataWriter.Reset();
                _dataWriter.Put((byte)packet.PacketType);
                packet.Serialize(_dataWriter);
                _netManager.DisconnectAll(_dataWriter.Data, 0, _dataWriter.Length);
            }
        }
        finally
        {
            PacketPool<VcLogoutRequestPacket>.Return(packet);
        }
    }

    public bool SendPacket<T>(NetPeer peer, T packet,
        DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered) where T : IVoiceCraftPacket
    {
        try
        {
            if (peer.ConnectionState != ConnectionState.Connected) return false;

            lock (_dataWriter)
            {
                _dataWriter.Reset();
                _dataWriter.Put((byte)packet.PacketType);
                packet.Serialize(_dataWriter);
                peer.Send(_dataWriter, deliveryMethod);
                return true;
            }
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    public bool SendUnconnectedPacket<T>(IPEndPoint remoteEndPoint, T packet) where T : IVoiceCraftPacket
    {
        try
        {
            lock (_dataWriter)
            {
                _dataWriter.Reset();
                _dataWriter.Put((byte)packet.PacketType);
                packet.Serialize(_dataWriter);
                return _netManager.SendUnconnectedMessage(_dataWriter, remoteEndPoint);
            }
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    public void Broadcast<T>(T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered,
        params NetPeer?[] excludes) where T : IVoiceCraftPacket
    {
        try
        {
            lock (_dataWriter)
            {
                var networkEntities = World.Entities.OfType<VoiceCraftNetworkEntity>();
                _dataWriter.Reset();
                _dataWriter.Put((byte)packet.PacketType);
                packet.Serialize(_dataWriter);
                foreach (var networkEntity in networkEntities)
                {
                    if (excludes.Contains(networkEntity.NetPeer)) continue;
                    networkEntity.NetPeer.Send(_dataWriter, deliveryMethod);
                }
            }
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        if (disposing) _netManager.Stop();

        _isDisposed = true;
    }

    //Packet Handling
    private static void ProcessPacket(VcPacketType packetType, NetPacketReader reader, NetPeer peer)
    {
        switch (packetType)
        {
            case VcPacketType.SetNameRequest:
                var setNameRequestPacket = PacketPool<VcSetNameRequestPacket>.GetPacket();
                setNameRequestPacket.Deserialize(reader);
                HandleSetNameRequestPacket(setNameRequestPacket, peer);
                break;
            case VcPacketType.AudioRequest:
                var audioRequestPacket = PacketPool<VcAudioRequestPacket>.GetPacket();
                audioRequestPacket.Deserialize(reader);
                HandleAudioRequestPacket(audioRequestPacket, peer);
                break;
            case VcPacketType.SetMuteRequest:
                var setMuteRequestPacket = PacketPool<VcSetMuteRequestPacket>.GetPacket();
                setMuteRequestPacket.Deserialize(reader);
                HandleSetMuteRequestPacket(setMuteRequestPacket, peer);
                break;
            case VcPacketType.SetDeafenRequest:
                var setDeafenRequestPacket = PacketPool<VcSetDeafenRequestPacket>.GetPacket();
                setDeafenRequestPacket.Deserialize(reader);
                HandleSetDeafenRequestPacket(setDeafenRequestPacket, peer);
                break;
            case VcPacketType.SetWorldIdRequest:
                var setWorldIdRequestPacket = PacketPool<VcSetWorldIdRequestPacket>.GetPacket();
                setWorldIdRequestPacket.Deserialize(reader);
                HandleSetWorldIdRequestPacket(setWorldIdRequestPacket, peer);
                break;
            case VcPacketType.SetPositionRequest:
                var setPositionRequestPacket = PacketPool<VcSetPositionRequestPacket>.GetPacket();
                setPositionRequestPacket.Deserialize(reader);
                HandleSetPositionRequestPacket(setPositionRequestPacket, peer);
                break;
            case VcPacketType.SetRotationRequest:
                var setRotationRequestPacket = PacketPool<VcSetRotationRequestPacket>.GetPacket();
                setRotationRequestPacket.Deserialize(reader);
                HandleSetRotationRequestPacket(setRotationRequestPacket, peer);
                break;
            case VcPacketType.SetCaveFactorRequest:
                var setCaveFactorRequestPacket = PacketPool<VcSetCaveFactorRequest>.GetPacket();
                setCaveFactorRequestPacket.Deserialize(reader);
                HandleSetCaveFactorRequestPacket(setCaveFactorRequestPacket, peer);
                break;
            case VcPacketType.SetMuffleFactorRequest:
                var setMuffleFactorRequestPacket = PacketPool<VcSetMuffleFactorRequest>.GetPacket();
                setMuffleFactorRequestPacket.Deserialize(reader);
                HandleSetMuffleFactorRequestPacket(setMuffleFactorRequestPacket, peer);
                break;
        }
    }

    private void ProcessUnconnectedPacket(VcPacketType packetType, NetPacketReader reader, IPEndPoint remoteEndPoint)
    {
        switch (packetType)
        {
            case VcPacketType.InfoRequest:
                var infoRequestPacket = PacketPool<VcInfoRequestPacket>.GetPacket();
                infoRequestPacket.Deserialize(reader);
                HandleInfoRequestPacket(infoRequestPacket, remoteEndPoint);
                break;
        }
    }

    private void HandleLoginRequestPacket(VcLoginRequestPacket packet, ConnectionRequest request)
    {
        try
        {
            if (packet.Version.Major != Version.Major || packet.Version.Minor != Version.Minor)
            {
                RejectRequest(request, "VoiceCraft.DisconnectReason.IncompatibleVersion");
                return;
            }

            if (packet.PositioningType != Config.PositioningType)
                switch (Config.PositioningType)
                {
                    case PositioningType.Server:
                        RejectRequest(request, "VoiceCraft.DisconnectReason.ServerSidedOnly");
                        return;
                    case PositioningType.Client:
                        RejectRequest(request, "VoiceCraft.DisconnectReason.ClientSidedOnly");
                        return;
                    default:
                        RejectRequest(request, "VoiceCraft.DisconnectReason.Error");
                        return;
                }

            var peer = request.Accept();
            try
            {
                World.CreateEntity(peer, packet.UserGuid, packet.ServerUserGuid, packet.Locale, packet.PositioningType,
                    false, false);
                SendPacket(peer, PacketPool<VcAcceptResponsePacket>.GetPacket().Set(packet.RequestId));
            }
            catch (Exception ex)
            {
                DisconnectPeer(peer, "VoiceCraft.DisconnectReason.Error");
                LogService.Log(ex);
            }
        }
        finally
        {
            PacketPool<VcLoginRequestPacket>.Return(packet);
        }
    }

    private void HandleInfoRequestPacket(VcInfoRequestPacket packet, IPEndPoint remoteEndPoint)
    {
        try
        {
            SendUnconnectedPacket(remoteEndPoint,
                PacketPool<VcInfoResponsePacket>.GetPacket().Set(Config.Motd, _netManager.ConnectedPeersCount,
                    Config.PositioningType, packet.Tick, Version));
        }
        finally
        {
            PacketPool<VcInfoRequestPacket>.Return(packet);
        }
    }

    private static void HandleSetNameRequestPacket(VcSetNameRequestPacket packet, NetPeer peer)
    {
        try
        {
            if (peer.Tag is not VoiceCraftNetworkEntity networkEntity) return;
            networkEntity.Name = packet.Value;
        }
        finally
        {
            PacketPool<VcSetNameRequestPacket>.Return(packet);
        }
    }

    private static void HandleAudioRequestPacket(VcAudioRequestPacket packet, NetPeer peer)
    {
        try
        {
            if (peer.Tag is not VoiceCraftNetworkEntity networkEntity || networkEntity.Muted ||
                networkEntity.ServerMuted) return;
            networkEntity.ReceiveAudio(packet.Data, packet.Timestamp, packet.FrameLoudness);
        }
        finally
        {
            PacketPool<VcAudioRequestPacket>.Return(packet);
        }
    }

    private static void HandleSetMuteRequestPacket(VcSetMuteRequestPacket packet, NetPeer peer)
    {
        try
        {
            if (peer.Tag is not VoiceCraftNetworkEntity networkEntity) return;
            networkEntity.Muted = packet.Value;
        }
        finally
        {
            PacketPool<VcSetMuteRequestPacket>.Return(packet);
        }
    }

    private static void HandleSetDeafenRequestPacket(VcSetDeafenRequestPacket packet, NetPeer peer)
    {
        try
        {
            if (peer.Tag is not VoiceCraftNetworkEntity networkEntity) return;
            networkEntity.Deafened = packet.Value;
        }
        finally
        {
            PacketPool<VcSetDeafenRequestPacket>.Return(packet);
        }
    }

    private static void HandleSetWorldIdRequestPacket(VcSetWorldIdRequestPacket packet, NetPeer peer)
    {
        try
        {
            if (peer.Tag is not VoiceCraftNetworkEntity
                {
                    PositioningType: PositioningType.Client
                } networkEntity) return;
            networkEntity.WorldId = packet.Value;
        }
        finally
        {
            PacketPool<VcSetWorldIdRequestPacket>.Return(packet);
        }
    }

    private static void HandleSetPositionRequestPacket(VcSetPositionRequestPacket packet, NetPeer peer)
    {
        try
        {
            if (peer.Tag is not VoiceCraftNetworkEntity
                {
                    PositioningType: PositioningType.Client
                } networkEntity) return;
            networkEntity.Position = packet.Value;
        }
        finally
        {
            PacketPool<VcSetPositionRequestPacket>.Return(packet);
        }
    }

    private static void HandleSetRotationRequestPacket(VcSetRotationRequestPacket packet, NetPeer peer)
    {
        try
        {
            if (peer.Tag is not VoiceCraftNetworkEntity
                {
                    PositioningType: PositioningType.Client
                } networkEntity) return;
            networkEntity.Rotation = packet.Value;
        }
        finally
        {
            PacketPool<VcSetRotationRequestPacket>.Return(packet);
        }
    }

    private static void HandleSetCaveFactorRequestPacket(VcSetCaveFactorRequest packet, NetPeer peer)
    {
        try
        {
            if (peer.Tag is not VoiceCraftNetworkEntity
                {
                    PositioningType: PositioningType.Client
                } networkEntity) return;
            networkEntity.CaveFactor = packet.Value;
        }
        finally
        {
            PacketPool<VcSetCaveFactorRequest>.Return(packet);
        }
    }

    private static void HandleSetMuffleFactorRequestPacket(VcSetMuffleFactorRequest packet, NetPeer peer)
    {
        try
        {
            if (peer.Tag is not VoiceCraftNetworkEntity
                {
                    PositioningType: PositioningType.Client
                } networkEntity) return;
            networkEntity.MuffleFactor = packet.Value;
        }
        finally
        {
            PacketPool<VcSetMuffleFactorRequest>.Return(packet);
        }
    }
}