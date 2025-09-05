using System.Diagnostics;
using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;
using Spectre.Console;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Network.Packets;
using VoiceCraft.Server.Config;
using VoiceCraft.Server.Data;
using VoiceCraft.Server.Systems;

namespace VoiceCraft.Server.Servers;

public class VoiceCraftServer : IResettable, IDisposable
{
    public static readonly Version Version = new(1, 1, 0);

    //Public Properties
    public VoiceCraftConfig Config { get; private set; } = new();
    public VoiceCraftWorld World { get; } = new();

    //Networking
    private readonly NetDataWriter _dataWriter = new();
    private readonly EventBasedNetListener _listener = new();
    private readonly AudioEffectSystem _audioEffectSystem = new();
    private readonly EventHandlerSystem _eventHandlerSystem;
    private readonly NetManager _netManager;

    //Systems
    private readonly VisibilitySystem _visibilitySystem;
    private bool _isDisposed;

    public VoiceCraftServer()
    {
        _netManager = new NetManager(_listener)
        {
            AutoRecycle = true,
            UnconnectedMessagesEnabled = true
        };

        _eventHandlerSystem = new EventHandlerSystem(this, World, _audioEffectSystem);
        _visibilitySystem = new VisibilitySystem(World, _audioEffectSystem);

        _listener.PeerDisconnectedEvent += OnPeerDisconnectedEvent;
        _listener.ConnectionRequestEvent += OnConnectionRequest;
        _listener.NetworkReceiveEvent += OnNetworkReceiveEvent;
        _listener.NetworkReceiveUnconnectedEvent += OnNetworkReceiveUnconnectedEvent;
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
        _visibilitySystem.Update();
        _eventHandlerSystem.Update();
    }

    public void Reset()
    {
        World.Reset();
        _audioEffectSystem.Reset();
    }

    public void Stop()
    {
        if (!_netManager.IsRunning) return;
        AnsiConsole.WriteLine(Locales.Locales.VoiceCraftServer_Stopping);
        DisconnectAll(new LogoutPacket("VoiceCraft.DisconnectReason.Shutdown"));
        _netManager.Stop();
        AnsiConsole.WriteLine(Locales.Locales.VoiceCraftServer_Stopped);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void RejectRequest<T>(ConnectionRequest request, T? packet = null) where T : VoiceCraftPacket
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

    public void DisconnectPeer<T>(NetPeer peer, T? packet = null) where T : VoiceCraftPacket
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
    
    public void DisconnectAll<T>(T? packet = null) where T : VoiceCraftPacket
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

    public bool SendPacket<T>(NetPeer peer, T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        where T : VoiceCraftPacket
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

    public bool SendPacket<T>(NetPeer[] peers, T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        where T : VoiceCraftPacket
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

    public bool SendUnconnectedPacket<T>(IPEndPoint remoteEndPoint, T packet) where T : VoiceCraftPacket
    {
        lock (_dataWriter)
        {
            _dataWriter.Reset();
            _dataWriter.Put((byte)packet.PacketType);
            packet.Serialize(_dataWriter);
            return _netManager.SendUnconnectedMessage(_dataWriter, remoteEndPoint);
        }
    }

    public void Broadcast<T>(T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered,
        params NetPeer?[] excludes)
        where T : VoiceCraftPacket
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
            World.Dispose();
            _netManager.Stop();
            _audioEffectSystem.Dispose();
            _eventHandlerSystem.Dispose();

            _listener.PeerDisconnectedEvent -= OnPeerDisconnectedEvent;
            _listener.ConnectionRequestEvent -= OnConnectionRequest;
            _listener.NetworkReceiveEvent -= OnNetworkReceiveEvent;
            _listener.NetworkReceiveUnconnectedEvent -= OnNetworkReceiveUnconnectedEvent;
        }

        _isDisposed = true;
    }

    //Packet Handling
    private void ProcessPacket(PacketType packetType, NetPacketReader reader, NetPeer? peer = null,
        IPEndPoint? remoteEndPoint = null)
    {
        switch (packetType)
        {
            case PacketType.Info:
                if (remoteEndPoint == null) return;
                var infoPacket = new InfoPacket();
                infoPacket.Deserialize(reader);
                HandleInfoPacket(infoPacket, remoteEndPoint);
                break;
            case PacketType.Audio:
                if (peer == null) return;
                var audioPacket = new AudioPacket();
                audioPacket.Deserialize(reader);
                HandleAudioPacket(audioPacket, peer);
                break;
            case PacketType.SetMute:
                if (peer == null) return;
                var setMutePacket = new SetMutePacket();
                setMutePacket.Deserialize(reader);
                HandleSetMutePacket(setMutePacket, peer);
                break;
            case PacketType.SetDeafen:
                if (peer == null) return;
                var setDeafenPacket = new SetDeafenPacket();
                setDeafenPacket.Deserialize(reader);
                HandleSetDeafenPacket(setDeafenPacket, peer);
                break;
            // Will need to implement these for client sided mode later.
            case PacketType.Unknown:
            case PacketType.Login:
            case PacketType.Logout:
            case PacketType.SetEffect:
            case PacketType.SetTitle:
            case PacketType.SetDescription:
            case PacketType.EntityCreated:
            case PacketType.NetworkEntityCreated:
            case PacketType.EntityDestroyed:
            case PacketType.SetVisibility:
            case PacketType.SetName:
            case PacketType.SetTalkBitmask:
            case PacketType.SetListenBitmask:
            case PacketType.SetPosition:
            case PacketType.SetRotation:
            default:
                break;
        }
    }

    private void HandleLoginPacket(LoginPacket packet, ConnectionRequest request)
    {
        if (packet.Version.Major != Version.Major || packet.Version.Minor != Version.Minor)
        {
            RejectRequest(request, new LogoutPacket("VoiceCraft.DisconnectReason.IncompatibleVersion"));
            return;
        }

        if (_netManager.ConnectedPeersCount >= Config.MaxClients)
        {
            RejectRequest(request, new LogoutPacket("VoiceCraft.DisconnectReason.ServerFull"));
            return;
        }

        var peer = request.Accept();
        try
        {
            var entity = new VoiceCraftNetworkEntity(peer, packet.UserGuid, packet.ServerUserGuid, packet.Locale,
                packet.PositioningType, World);
            peer.Tag = entity;
            World.AddEntity(entity);
        }
        catch
        {
            DisconnectPeer(peer, new LogoutPacket("VoiceCraft.DisconnectReason.Error"));
        }
    }

    private void HandleInfoPacket(InfoPacket packet, IPEndPoint remoteEndPoint)
    {
        SendUnconnectedPacket(remoteEndPoint,
            new InfoPacket(Config.Motd, _netManager.ConnectedPeersCount, Config.PositioningType, packet.Tick));
    }

    private void HandleAudioPacket(AudioPacket packet, NetPeer peer)
    {
        var entity = World.GetEntity(peer.Id);
        if (entity is not VoiceCraftNetworkEntity networkEntity) return;
        networkEntity.ReceiveAudio(packet.Data, packet.Timestamp, packet.FrameLoudness);
    }

    private void HandleSetMutePacket(SetMutePacket packet, NetPeer peer)
    {
        var entity = World.GetEntity(peer.Id);
        if (entity is not VoiceCraftNetworkEntity) return;
        entity.Muted = packet.Value;
    }

    private void HandleSetDeafenPacket(SetDeafenPacket packet, NetPeer peer)
    {
        var entity = World.GetEntity(peer.Id);
        if (entity is not VoiceCraftNetworkEntity) return;
        entity.Deafened = packet.Value;
    }

    private void OnPeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        if (peer.Tag is not VoiceCraftNetworkEntity) return;
        World.DestroyEntity(peer.Id);
    }

    private void OnConnectionRequest(ConnectionRequest request)
    {
        if (request.Data.IsNull)
        {
            request.Reject();
            return;
        }

        try
        {
            var loginPacket = new LoginPacket();
            loginPacket.Deserialize(request.Data);
            HandleLoginPacket(loginPacket, request);
        }
        catch
        {
            RejectRequest(request, new LogoutPacket("VoiceCraft.DisconnectReason.Error"));
        }
    }

    private void OnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel,
        DeliveryMethod deliveryMethod)
    {
        try
        {
            var packetType = reader.GetByte();
            var pt = (PacketType)packetType;
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
            var pt = (PacketType)packetType;
            ProcessPacket(pt, reader, remoteEndPoint: remoteEndPoint);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }
}