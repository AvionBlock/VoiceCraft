using System.Diagnostics;
using System.Net;
using LiteNetLib;
using VoiceCraft.Core;
using VoiceCraft.Core.Network.Packets;
using VoiceCraft.Server.Application;
using VoiceCraft.Server.Config;
using VoiceCraft.Server.Data;

namespace VoiceCraft.Server.Systems;

public class NetworkSystem : IDisposable
{
    private readonly VoiceCraftConfig _config;
    private readonly EventBasedNetListener _listener;
    private readonly NetManager _netManager;
    private readonly VoiceCraftServer _server;
    private readonly VoiceCraftWorld _world;

    public NetworkSystem(VoiceCraftServer server, VoiceCraftWorld world, EventBasedNetListener listener, NetManager netManager)
    {
        _server = server;
        _config = server.Config;
        _world = world;
        _listener = listener;
        _netManager = netManager;

        _listener.PeerDisconnectedEvent += OnPeerDisconnectedEvent;
        _listener.ConnectionRequestEvent += OnConnectionRequest;
        _listener.NetworkReceiveEvent += OnNetworkReceiveEvent;
        _listener.NetworkReceiveUnconnectedEvent += OnNetworkReceiveUnconnectedEvent;
    }

    public void Dispose()
    {
        _listener.PeerDisconnectedEvent -= OnPeerDisconnectedEvent;
        _listener.ConnectionRequestEvent -= OnConnectionRequest;
        _listener.NetworkReceiveEvent -= OnNetworkReceiveEvent;
        _listener.NetworkReceiveUnconnectedEvent -= OnNetworkReceiveUnconnectedEvent;
        GC.SuppressFinalize(this);
    }

    private void OnPeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        if (peer.Tag is not VoiceCraftNetworkEntity) return;
        _world.DestroyEntity(peer.Id);
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
            request.Reject("An error occurred on the server while trying to parse the login!"u8.ToArray());
        }
    }

    private void OnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
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

    private void OnNetworkReceiveUnconnectedEvent(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
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

    private void ProcessPacket(PacketType packetType, NetPacketReader reader, NetPeer? peer = null, IPEndPoint? remoteEndPoint = null)
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
            case PacketType.SetTitle:
            case PacketType.SetDescription:
            case PacketType.SetEffect:
            case PacketType.EntityCreated:
            case PacketType.EntityDestroyed:
            case PacketType.SetVisibility:
            case PacketType.SetName:
            case PacketType.SetTalkBitmask:
            case PacketType.SetListenBitmask:
            case PacketType.SetPosition:
            case PacketType.SetRotation:
            case PacketType.SetProperty:
            default:
                break;
        }
    }

    //Packet Handling
    private void HandleLoginPacket(LoginPacket packet, ConnectionRequest request)
    {
        if (!Enum.IsDefined(packet.LoginType))
        {
            request.Reject("Unknown login type!"u8.ToArray());
            return;
        }

        if (!Version.TryParse(packet.Version, out var version) || version.Major != VoiceCraftServer.Version.Major)
        {
            request.Reject("Incompatible client/server version!"u8.ToArray()); //Will need to change these so it displays as a locale on the client.
            return;
        }

        if (_netManager.ConnectedPeersCount >= _config.MaxClients)
        {
            request.Reject("Server is full! Maximum clients reached!"u8.ToArray());
            return;
        }

        if (packet.LoginType == LoginType.Unknown)
        {
            request.Reject("Unknown login type!"u8.ToArray());
            return;
        }

        var peer = request.Accept();
        try
        {
            switch (packet.LoginType)
            {
                case LoginType.Login:
                    var entity = new VoiceCraftNetworkEntity(peer, packet.UserGuid, packet.Locale, packet.PositioningType, _world);
                    _world.AddEntity(entity);
                    peer.Tag = entity;
                    break;
                case LoginType.Discovery:
                    peer.Tag = LoginType.Discovery;
                    break;
                case LoginType.Unknown:
                default:
                    peer.Disconnect("Unknown login type!"u8.ToArray());
                    break;
            }
        }
        catch
        {
            peer.Disconnect("An error occurred on the server while trying to parse the login!"u8.ToArray());
        }
    }

    private void HandleInfoPacket(InfoPacket infoPacket, IPEndPoint remoteEndPoint)
    {
        _server.SendUnconnectedPacket(remoteEndPoint,
            new InfoPacket(_config.Motd, _netManager.ConnectedPeersCount, _config.PositioningType, infoPacket.Tick));
    }

    private void HandleAudioPacket(AudioPacket packet, NetPeer peer)
    {
        var entity = _world.GetEntity(peer.Id);
        if (entity is not VoiceCraftNetworkEntity networkEntity) return;
        networkEntity.ReceiveAudio(packet.Data, packet.Timestamp, packet.FrameLoudness);
    }

    private void HandleSetMutePacket(SetMutePacket packet, NetPeer peer)
    {
        var entity = _world.GetEntity(peer.Id);
        if (entity is not VoiceCraftNetworkEntity) return;
        entity.Muted = packet.Value;
    }

    private void HandleSetDeafenPacket(SetDeafenPacket packet, NetPeer peer)
    {
        var entity = _world.GetEntity(peer.Id);
        if (entity is not VoiceCraftNetworkEntity) return;
        entity.Deafened = packet.Value;
    }
}