using System.Diagnostics;
using System.Net;
using LiteNetLib;
using VoiceCraft.Core;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.Packets;
using VoiceCraft.Server.Application;
using VoiceCraft.Server.Config;
using VoiceCraft.Server.Data;

namespace VoiceCraft.Server.Systems
{
    public class NetworkSystem : IDisposable
    {
        private readonly VoiceCraftServer _server;
        private readonly VoiceCraftWorld _world;
        private readonly EventBasedNetListener _listener;
        private readonly NetManager _netManager;
        private readonly VoiceCraftConfig _config;

        public NetworkSystem(VoiceCraftServer server, NetManager netManager)
        {
            _server = server;
            _world = server.World;
            _listener = server.Listener;
            _config = server.Config;
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
            _world.DestroyEntity((byte)peer.Id);
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
                if (Version.Parse(loginPacket.Version).Major != VoiceCraftServer.Version.Major)
                {
                    request.Reject("Incompatible client/server version!"u8.ToArray());
                    return;
                }
                if (_world.Entities.Count() >= byte.MaxValue)
                {
                    request.Reject("Server full!"u8.ToArray());
                    return;
                }

                HandleLogin(loginPacket, request);
            }
            catch
            {
                request.Reject("An error occurred on the server while trying to parse the login!"u8.ToArray());
            }
        }

        private void HandleLogin(LoginPacket loginPacket, ConnectionRequest request)
        {
            if (loginPacket.LoginType == LoginType.Unknown)
            {
                request.Reject("Unknown login type!"u8.ToArray());
                return;
            }

            var peer = request.Accept();
            try
            {
                switch (loginPacket.LoginType)
                {
                    case LoginType.Login:
                        if (peer.Id > byte.MaxValue)
                            throw new InvalidOperationException();

                        var entity = new VoiceCraftNetworkEntity(peer);
                        _world.AddEntity(entity);
                        peer.Tag = entity;
                        break;
                    case LoginType.Discovery:
                        peer.Tag = LoginType.Discovery;
                        break;
                    case LoginType.Unknown:
                    default:
                        request.Reject();
                        break;
                }
            }
            catch
            {
                peer.Disconnect("An error occurred on the server while trying to parse the login!"u8.ToArray());
            }
        }

        private void OnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            try
            {
                var packetType = reader.GetByte();
                var pt = (PacketType)packetType;
                switch (pt)
                {
                    case PacketType.Audio:
                        var audioPacket = new AudioPacket();
                        audioPacket.Deserialize(reader);
                        HandleAudioPacket(audioPacket, peer);
                        break;
                    // Will need to implement these for client sided mode later.
                    case PacketType.Info:
                    case PacketType.Login:
                    case PacketType.SetTitle:
                    case PacketType.SetDescription:
                    case PacketType.SetEffect:
                    case PacketType.RemoveEffect:
                    case PacketType.EntityCreated:
                    case PacketType.EntityReset:
                    case PacketType.EntityDestroyed:
                    case PacketType.SetName:
                    case PacketType.SetTalkBitmask:
                    case PacketType.SetListenBitmask:
                    case PacketType.SetMinRange:
                    case PacketType.SetMaxRange:
                    case PacketType.SetPosition:
                    case PacketType.SetRotation:
                    case PacketType.SetProperty:
                    case PacketType.RemoveProperty:
                    case PacketType.Unknown:
                    default:
                        break;
                }
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
                switch (pt)
                {
                    case PacketType.Info:
                        var infoPacket = new InfoPacket();
                        infoPacket.Deserialize(reader);
                        HandleInfoPacket(infoPacket, remoteEndPoint);
                        break;
                    //Unused
                    case PacketType.Login:
                    case PacketType.Audio:
                    case PacketType.SetTitle:
                    case PacketType.SetDescription:
                    case PacketType.SetEffect:
                    case PacketType.RemoveEffect:
                    case PacketType.EntityCreated:
                    case PacketType.EntityReset:
                    case PacketType.EntityDestroyed:
                    case PacketType.SetName:
                    case PacketType.SetTalkBitmask:
                    case PacketType.SetListenBitmask:
                    case PacketType.SetMinRange:
                    case PacketType.SetMaxRange:
                    case PacketType.SetPosition:
                    case PacketType.SetRotation:
                    case PacketType.SetProperty:
                    case PacketType.RemoveProperty:
                    case PacketType.Unknown:
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        //Packet Handling
        private void HandleInfoPacket(InfoPacket infoPacket, IPEndPoint remoteEndPoint)
        {
            var packet = new InfoPacket(_config.Motd, _netManager.ConnectedPeersCount, _config.Discovery, _config.PositioningType, infoPacket.Tick);
            _server.SendUnconnectedPacket(remoteEndPoint, packet);
        }

        private void HandleAudioPacket(AudioPacket audioPacket, NetPeer peer)
        {
            var entity = _world.GetEntity((byte)peer.Id);
            if (entity is not VoiceCraftNetworkEntity networkEntity) return;
            networkEntity.ReceiveAudio(audioPacket.Data, audioPacket.Timestamp);
        }
    }
}