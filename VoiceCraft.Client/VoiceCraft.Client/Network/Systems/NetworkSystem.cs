using System;
using System.Diagnostics;
using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Client.Network.Systems
{
    public class NetworkSystem : IDisposable
    {
        private readonly VoiceCraftClient _client;
        private readonly EventBasedNetListener _listener;
        private readonly NetDataWriter _dataWriter;
        private readonly NetManager _netManager;
        private readonly VoiceCraftWorld _world;

        public event Action<ServerInfo>? OnServerInfo;
        public event Action<string>? OnSetTitle;

        public NetworkSystem(VoiceCraftClient client, NetManager netManager)
        {
            _dataWriter = new NetDataWriter();
            _client = client;
            _netManager = netManager;
            _listener = _client.Listener;
            _world = _client.World;

            _listener.ConnectionRequestEvent += OnConnectionRequestEvent;
            _listener.NetworkReceiveEvent += OnNetworkReceiveEvent;
            _listener.NetworkReceiveUnconnectedEvent += OnNetworkReceiveUnconnectedEvent;
        }

        public bool SendPacket<T>(T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered) where T : VoiceCraftPacket
        {
            if (_client.ConnectionState != ConnectionState.Connected) return false;

            lock (_dataWriter)
            {
                _dataWriter.Reset();
                _dataWriter.Put((byte)packet.PacketType);
                packet.Serialize(_dataWriter);
                _client.LocalEntity?.NetPeer.Send(_dataWriter, deliveryMethod);
                return true;
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
        
        public bool SendUnconnectedPacket<T>(string ip, uint port, T packet) where T : VoiceCraftPacket
        {
            lock (_dataWriter)
            {
                _dataWriter.Reset();
                _dataWriter.Put((byte)packet.PacketType);
                packet.Serialize(_dataWriter);
                return _netManager.SendUnconnectedMessage(_dataWriter, ip, (int)port);
            }
        }
        
        public void Dispose()
        {
            _listener.ConnectionRequestEvent -= OnConnectionRequestEvent;
            _listener.NetworkReceiveEvent -= OnNetworkReceiveEvent;
            _listener.NetworkReceiveUnconnectedEvent -= OnNetworkReceiveUnconnectedEvent;
            GC.SuppressFinalize(this);
        }

        private static void OnConnectionRequestEvent(ConnectionRequest request)
        {
            request.Reject(); //No fuck you.
        }

        private void OnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliverymethod)
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
                        HandleAudioPacket(audioPacket);
                        break;
                    case PacketType.SetTitle:
                        var setTitlePacket = new SetTitlePacket();
                        setTitlePacket.Deserialize(reader);
                        HandleSetTitlePacket(setTitlePacket);
                        break;
                    case PacketType.EntityCreated:
                        var entityCreatedPacket = new EntityCreatedPacket();
                        entityCreatedPacket.Deserialize(reader);
                        HandleEntityCreatedPacket(entityCreatedPacket);
                        break;
                    case PacketType.EntityDestroyed:
                        var entityDestroyedPacket = new EntityDestroyedPacket();
                        entityDestroyedPacket.Deserialize(reader);
                        HandleEntityDestroyedPacket(entityDestroyedPacket);
                        break;
                    case PacketType.Info:
                    case PacketType.Login:
                    case PacketType.SetEffect:
                    case PacketType.RemoveEffect:
                    case PacketType.SetName:
                    case PacketType.SetTalkBitmask:
                    case PacketType.SetListenBitmask:
                    case PacketType.SetPosition:
                    case PacketType.SetRotation:
                    case PacketType.SetIntProperty:
                    case PacketType.SetBoolProperty:
                    case PacketType.SetFloatProperty:
                    case PacketType.RemoveIntProperty:
                    case PacketType.RemoveBoolProperty:
                    case PacketType.RemoveFloatProperty:
                    case PacketType.Unknown:
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            
            reader.Recycle();
        }

        private void OnNetworkReceiveUnconnectedEvent(IPEndPoint remoteendpoint, NetPacketReader reader, UnconnectedMessageType messagetype)
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
                        HandleInfoPacket(infoPacket);
                        break;
                    //Unused
                    case PacketType.Login:
                    case PacketType.Audio:
                    case PacketType.SetTitle:
                    case PacketType.SetEffect:
                    case PacketType.RemoveEffect:
                    case PacketType.EntityCreated:
                    case PacketType.EntityDestroyed:
                    case PacketType.SetName:
                    case PacketType.SetTalkBitmask:
                    case PacketType.SetListenBitmask:
                    case PacketType.SetPosition:
                    case PacketType.SetRotation:
                    case PacketType.SetIntProperty:
                    case PacketType.SetBoolProperty:
                    case PacketType.SetFloatProperty:
                    case PacketType.RemoveIntProperty:
                    case PacketType.RemoveBoolProperty:
                    case PacketType.RemoveFloatProperty:
                    case PacketType.Unknown:
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            
            reader.Recycle();
        }

        private void HandleInfoPacket(InfoPacket infoPacket)
        {
            OnServerInfo?.Invoke(new ServerInfo(infoPacket));
        }
        
        private void HandleAudioPacket(AudioPacket packet)
        {
            if (!_world.Entities.TryGetValue(packet.Id, out var entity)) return;
            entity.ReceiveAudio(packet.Data, packet.Timestamp);
        }

        private void HandleSetTitlePacket(SetTitlePacket packet)
        {
            OnSetTitle?.Invoke(packet.Title);
        }

        private void HandleEntityCreatedPacket(EntityCreatedPacket packet)
        {
            try
            {
                if (_world.Entities.ContainsKey(packet.Id)) return;
                _world.AddEntity(packet.Entity); //Could crash the application, this shouldn't happen though.
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void HandleEntityDestroyedPacket(EntityDestroyedPacket packet)
        {
            _world.DestroyEntity(packet.Id); //Won't crash.
        }
    }
}