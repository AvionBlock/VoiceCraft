using System.Diagnostics;
using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;
using Spectre.Console;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Network.VcPackets;
using VoiceCraft.Core.Network.VcPackets.Event;
using VoiceCraft.Core.Network.VcPackets.Request;
using VoiceCraft.Core.World;
using VoiceCraft.Server.Config;
using VoiceCraft.Server.Systems;

namespace VoiceCraft.Server.Servers;

public class VoiceCraftServer : IResettable, IDisposable
{
    public static readonly Version Version = new(1, 1, 0);

    //Networking
    private readonly NetDataWriter _dataWriter = new();
    private readonly EventBasedNetListener _listener = new();
    private readonly NetManager _netManager;

    //Systems
    private readonly AudioEffectSystem _audioEffectSystem;
    private bool _isDisposed;

    public VoiceCraftServer(AudioEffectSystem audioEffectSystem, VoiceCraftWorld world)
    {
        _netManager = new NetManager(_listener)
        {
            AutoRecycle = true,
            UnconnectedMessagesEnabled = true
        };

        _audioEffectSystem = audioEffectSystem;
        World = world;

        _listener.PeerDisconnectedEvent += OnPeerDisconnectedEvent;
        _listener.ConnectionRequestEvent += OnConnectionRequest;
        _listener.NetworkReceiveEvent += OnNetworkReceiveEvent;
        _listener.NetworkReceiveUnconnectedEvent += OnNetworkReceiveUnconnectedEvent;
    }

    //Public Properties
    public VoiceCraftConfig Config { get; private set; } = new();
    public VoiceCraftWorld World { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Reset()
    {
        World.Reset();
        _audioEffectSystem.Reset();
    }

    ~VoiceCraftServer()
    {
        Dispose(false);
    }

    public void Start(VoiceCraftConfig? config = null)
    {
        Stop();

        AnsiConsole.WriteLine(Locales.Locales.VoiceCraftServer_Starting);
        if (config != null)
            Config = config;

        if (_netManager.IsRunning || _netManager.Start((int)Config.Port))
            AnsiConsole.MarkupLine($"[green]{Locales.Locales.VoiceCraftServer_Success}[/]");
        else
            throw new Exception(Locales.Locales.VoiceCraftServer_Exceptions_Failed);
    }

    public void Update()
    {
        _netManager.PollEvents();
    }

    public void Stop()
    {
        if (!_netManager.IsRunning) return;
        AnsiConsole.WriteLine(Locales.Locales.VoiceCraftServer_Stopping);
        DisconnectAll(PacketPool<VcLogoutRequestPacket>.GetPacket().Set("VoiceCraft.DisconnectReason.Shutdown"));
        _netManager.Stop();
        AnsiConsole.WriteLine(Locales.Locales.VoiceCraftServer_Stopped);
    }

    public void RejectRequest(ConnectionRequest request, IVoiceCraftPacket? packet = null)
    {
        if (packet == null)
        {
            request.Reject();
            return;
        }

        lock (_dataWriter)
        {
            _dataWriter.Reset();
            packet.Serialize(_dataWriter);
            request.Reject(_dataWriter);
        }
    }

    public void DisconnectPeer(NetPeer peer, IVoiceCraftPacket? packet = null)
    {
        if (packet == null)
        {
            peer.Disconnect();
            return;
        }

        lock (_dataWriter)
        {
            _dataWriter.Reset();
            packet.Serialize(_dataWriter);
            peer.Disconnect(_dataWriter);
        }
    }

    public void DisconnectAll(IVoiceCraftPacket? packet = null)
    {
        if (packet == null)
        {
            _netManager.DisconnectAll();
            return;
        }

        lock (_dataWriter)
        {
            _dataWriter.Reset();
            _dataWriter.Put((byte)packet.PacketType);
            packet.Serialize(_dataWriter);
            _netManager.DisconnectAll(_dataWriter.Data, 0, _dataWriter.Length);
        }
    }

    public bool SendPacket(NetPeer peer, IVoiceCraftPacket packet,
        DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
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

    public bool SendPacket(NetPeer[] peers, IVoiceCraftPacket packet,
        DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
    {
        lock (_dataWriter)
        {
            _dataWriter.Reset();
            _dataWriter.Put((byte)packet.PacketType);
            packet.Serialize(_dataWriter);

            var status = true;
            foreach (var peer in peers)
            {
                if (peer.ConnectionState != ConnectionState.Connected)
                {
                    status = false;
                    continue;
                }

                peer.Send(_dataWriter, deliveryMethod);
            }

            return status;
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

    public void Broadcast(IVoiceCraftPacket packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered,
        params NetPeer?[] excludes)
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

    private void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        if (disposing)
        {
            _netManager.Stop();
            _listener.PeerDisconnectedEvent -= OnPeerDisconnectedEvent;
            _listener.ConnectionRequestEvent -= OnConnectionRequest;
            _listener.NetworkReceiveEvent -= OnNetworkReceiveEvent;
            _listener.NetworkReceiveUnconnectedEvent -= OnNetworkReceiveUnconnectedEvent;
        }

        _isDisposed = true;
    }

    //Packet Handling
    private void ProcessPacket(VcPacketType packetType, NetPacketReader reader, NetPeer? peer = null,
        IPEndPoint? remoteEndPoint = null)
    {
        switch (packetType)
        {
            case VcPacketType.InfoRequest:
                if (remoteEndPoint == null) return;
                var infoRequestPacket = PacketPool<VcInfoRequestPacket>.GetPacket();
                infoRequestPacket.Deserialize(reader);
                HandleInfoRequestPacket(infoRequestPacket, remoteEndPoint);
                break;
            case VcPacketType.AudioRequest:
                if (peer == null) return;
                var audioRequestPacket = PacketPool<VcAudioRequestPacket>.GetPacket();
                audioRequestPacket.Deserialize(reader);
                HandleAudioRequestPacket(audioRequestPacket, peer);
                break;
            case VcPacketType.SetMuteRequest:
                if (peer == null) return;
                var setMuteRequestPacket = PacketPool<VcSetMuteRequestPacket>.GetPacket();
                setMuteRequestPacket.Deserialize(reader);
                HandleSetMuteRequestPacket(setMuteRequestPacket, peer);
                break;
            case VcPacketType.SetDeafenRequest:
                if (peer == null) return;
                var setDeafenRequestPacket = PacketPool<VcSetDeafenRequestPacket>.GetPacket();
                setDeafenRequestPacket.Deserialize(reader);
                HandleSetDeafenRequestPacket(setDeafenRequestPacket, peer);
                break;
        }
    }

    private void HandleLoginRequestPacket(VcLoginRequestPacket packet, ConnectionRequest request)
    {
        if (packet.Version.Major != Version.Major || packet.Version.Minor != Version.Minor)
        {
            RejectRequest(request, new LogoutPacket("VoiceCraft.DisconnectReason.IncompatibleVersion"));
            return;
        }

        var peer = request.Accept();
        try
        {
            World.CreateEntity(peer, packet.UserGuid, packet.ServerUserGuid, packet.Locale, packet.PositioningType);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            DisconnectPeer(peer, new LogoutPacket("VoiceCraft.DisconnectReason.Error"));
        }
    }

    private void HandleInfoRequestPacket(VcInfoRequestPacket packet, IPEndPoint remoteEndPoint)
    {
        try
        {
            SendUnconnectedPacket(remoteEndPoint,
                new VcInfoRequestPacket(Config.Motd, _netManager.ConnectedPeersCount, Config.PositioningType,
                    packet.Tick));
        }
        finally
        {
            PacketPool<VcInfoRequestPacket>.Return(packet);
        }
    }

    private static void HandleAudioRequestPacket(VcAudioRequestPacket packet, NetPeer peer)
    {
        try
        {
            if (peer.Tag is not VoiceCraftNetworkEntity networkEntity) return;
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

    private void OnPeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        if (peer.Tag is not VoiceCraftNetworkEntity networkEntity) return;
        World.DestroyEntity(networkEntity.Id);
    }

    private void OnConnectionRequest(ConnectionRequest request)
    {
        if (_netManager.ConnectedPeersCount >= Config.MaxClients)
        {
            RejectRequest(request, new LogoutPacket("VoiceCraft.DisconnectReason.ServerFull"));
            return;
        }

        if (request.Data.IsNull)
        {
            RejectRequest(request, new LogoutPacket("VoiceCraft.DisconnectReason.Forced"));
            return;
        }

        try
        {
            var loginPacket = new LoginPacket();
            loginPacket.Deserialize(request.Data);
            HandleLoginRequestPacket(loginPacket, request);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            RejectRequest(request, new LogoutPacket("VoiceCraft.DisconnectReason.Error"));
        }
    }

    private void OnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel,
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
            Debug.WriteLine(ex);
        }
    }

    private void OnNetworkReceiveUnconnectedEvent(IPEndPoint remoteEndPoint, NetPacketReader reader,
        UnconnectedMessageType messageType)
    {
        try
        {
            var packetType = reader.GetByte();
            var pt = (VcPacketType)packetType;
            ProcessPacket(pt, reader, remoteEndPoint: remoteEndPoint);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }
}