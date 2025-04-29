using System;
using System.Diagnostics;
using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Client.Network.Systems
{
    public class NetworkSystem : IDisposable
    {
        private readonly VoiceCraftClient _client;
        private readonly EventBasedNetListener _listener;
        private readonly VoiceCraftWorld _world;

        public event Action<ServerInfo>? OnServerInfo;
        public event Action<string>? OnSetTitle;
        public event Action<string>? OnSetDescription;

        public NetworkSystem(VoiceCraftClient client)
        {
            _client = client;
            _listener = client.Listener;
            _world = client.World;

            _listener.ConnectionRequestEvent += OnConnectionRequestEvent;
            _listener.NetworkReceiveEvent += OnNetworkReceiveEvent;
            _listener.NetworkReceiveUnconnectedEvent += OnNetworkReceiveUnconnectedEvent;
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

        private void OnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            try
            {
                var packetType = reader.GetByte();
                var pt = (PacketType)packetType;
                ProcessPacket(pt, reader);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            reader.Recycle();
        }

        private void OnNetworkReceiveUnconnectedEvent(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            try
            {
                var packetType = reader.GetByte();
                var pt = (PacketType)packetType;
                ProcessPacket(pt, reader);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            reader.Recycle();
        }

        private void ProcessPacket(PacketType packetType, NetPacketReader reader)
        {
            switch (packetType)
            {
                case PacketType.Info:
                    var infoPacket = new InfoPacket();
                    infoPacket.Deserialize(reader);
                    HandleInfoPacket(infoPacket);
                    break;
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
                case PacketType.SetDescription:
                    var setDescriptionPacket = new SetDescriptionPacket();
                    setDescriptionPacket.Deserialize(reader);
                    HandleSetDescriptionPacket(setDescriptionPacket);
                    break;
                case PacketType.SetMute:
                    var setMutePacket = new SetMutePacket();
                    setMutePacket.Deserialize(reader);
                    HandleSetMutePacket(setMutePacket);
                    break;
                case PacketType.SetDeafen:
                    var setDeafen = new SetDeafenPacket();
                    setDeafen.Deserialize(reader);
                    HandleSetDeafenPacket(setDeafen);
                    break;
                case PacketType.SetMinRange:
                    var setMinRangePacket = new SetMinRangePacket();
                    setMinRangePacket.Deserialize(reader);
                    HandleSetMinRangePacket(setMinRangePacket);
                    break;
                case PacketType.SetMaxRange:
                    var setMaxRangePacket = new SetMaxRangePacket();
                    setMaxRangePacket.Deserialize(reader);
                    HandleSetMaxRangePacket(setMaxRangePacket);
                    break;
                case PacketType.EntityCreated:
                    var entityCreatedPacket = new EntityCreatedPacket();
                    entityCreatedPacket.Deserialize(reader);
                    HandleEntityCreatedPacket(entityCreatedPacket, reader);
                    break;
                case PacketType.EntityDestroyed:
                    var entityDestroyedPacket = new EntityDestroyedPacket();
                    entityDestroyedPacket.Deserialize(reader);
                    HandleEntityDestroyedPacket(entityDestroyedPacket);
                    break;
                case PacketType.SetVisibility:
                    var setVisibilityPacket = new SetVisibilityPacket();
                    setVisibilityPacket.Deserialize(reader);
                    HandleSetVisibilityPacket(setVisibilityPacket);
                    break;
                case PacketType.SetName:
                    var setNamePacket = new SetNamePacket();
                    setNamePacket.Deserialize(reader);
                    HandleSetNamePacket(setNamePacket);
                    break;
                case PacketType.SetTalkBitmask:
                    var setTalkBitmaskPacket = new SetTalkBitmaskPacket();
                    setTalkBitmaskPacket.Deserialize(reader);
                    HandleSetTalkBitmaskPacket(setTalkBitmaskPacket);
                    break;
                case PacketType.SetListenBitmask:
                    var setListenBitmaskPacket = new SetListenBitmaskPacket();
                    setListenBitmaskPacket.Deserialize(reader);
                    HandleSetListenBitmaskPacket(setListenBitmaskPacket);
                    break;
                case PacketType.SetPosition:
                    var setPositionPacket = new SetPositionPacket();
                    setPositionPacket.Deserialize(reader);
                    HandleSetPositionPacket(setPositionPacket);
                    break;
                case PacketType.SetRotation:
                    var setRotationPacket = new SetRotationPacket();
                    setRotationPacket.Deserialize(reader);
                    HandleSetRotationPacket(setRotationPacket);
                    break;
                case PacketType.SetProperty:
                    var setPropertyPacket = new SetPropertyPacket();
                    setPropertyPacket.Deserialize(reader);
                    HandleSetPropertyPacket(setPropertyPacket);
                    break;
                case PacketType.Login:
                case PacketType.SetEffect:
                case PacketType.Unknown:
                default:
                    break;
            }
        }

        private void HandleInfoPacket(InfoPacket infoPacket)
        {
            OnServerInfo?.Invoke(new ServerInfo(infoPacket));
        }

        private void HandleAudioPacket(AudioPacket packet)
        {
            var entity = _world.GetEntity(packet.Id);
            entity?.ReceiveAudio(packet.Data, packet.Timestamp, packet.FrameLoudness);
        }

        private void HandleSetTitlePacket(SetTitlePacket packet)
        {
            OnSetTitle?.Invoke(packet.Value);
        }

        private void HandleSetDescriptionPacket(SetDescriptionPacket packet)
        {
            OnSetDescription?.Invoke(packet.Value);
        }
        
        private void HandleSetMinRangePacket(SetMinRangePacket packet)
        {
            _world.MinRange = packet.Value;
        }

        private void HandleSetMaxRangePacket(SetMaxRangePacket packet)
        {
            _world.MaxRange = packet.Value;
        }

        private void HandleEntityCreatedPacket(EntityCreatedPacket packet, NetDataReader reader)
        {
            var entity = new VoiceCraftClientEntity(packet.Id, _world);
            entity.Deserialize(reader);
            _world.AddEntity(entity);
        }

        private void HandleEntityDestroyedPacket(EntityDestroyedPacket packet)
        {
            _world.DestroyEntity(packet.Id);
        }

        private void HandleSetVisibilityPacket(SetVisibilityPacket packet)
        {
            var entity = _world.GetEntity(packet.Id);
            if (entity is not VoiceCraftClientEntity clientEntity) return;
            clientEntity.IsVisible = packet.Value;
            if (clientEntity.IsVisible) return; //Clear properties and the audio buffer when entity is not visible.
            clientEntity.ClearBuffer();
            clientEntity.ClearProperties();
        }

        private void HandleSetNamePacket(SetNamePacket packet)
        {
            if (packet.Id == _client.Id)
            {
                _client.Name = packet.Value;
                return;
            }

            var entity = _world.GetEntity(packet.Id);
            if (entity == null) return;
            entity.Name = packet.Value;
        }

        private void HandleSetMutePacket(SetMutePacket packet)
        {
            if (packet.Id == _client.Id)
            {
                _client.Muted = packet.Value;
                return;
            }
            
            var entity = _world.GetEntity(packet.Id);
            if (entity == null) return;
            entity.Muted = packet.Value;
        }
        
        private void HandleSetDeafenPacket(SetDeafenPacket packet)
        {
            if (packet.Id == _client.Id)
            {
                _client.Deafened = packet.Value;
                return;
            }
            
            var entity = _world.GetEntity(packet.Id);
            if (entity == null) return;
            entity.Deafened = packet.Value;
        }

        private void HandleSetTalkBitmaskPacket(SetTalkBitmaskPacket packet)
        {
            if (packet.Id == _client.Id)
            {
                _client.TalkBitmask = packet.Value;
                return;
            }
            
            var entity = _world.GetEntity(packet.Id);
            if (entity == null) return;
            entity.TalkBitmask = packet.Value;
        }

        private void HandleSetListenBitmaskPacket(SetListenBitmaskPacket packet)
        {
            if (packet.Id == _client.Id)
            {
                _client.ListenBitmask = packet.Value;
                return;
            }
            
            var entity = _world.GetEntity(packet.Id);
            if (entity == null) return;
            entity.ListenBitmask = packet.Value;
        }

        private void HandleSetPositionPacket(SetPositionPacket packet)
        {
            if (packet.Id == _client.Id)
            {
                _client.Position = packet.Value;
                return;
            }
            
            var entity = _world.GetEntity(packet.Id);
            if (entity == null) return;
            entity.Position = packet.Value;
        }

        private void HandleSetRotationPacket(SetRotationPacket packet)
        {
            if (packet.Id == _client.Id)
            {
                _client.Rotation = packet.Value;
                return;
            }
            
            var entity = _world.GetEntity(packet.Id);
            if (entity == null) return;
            entity.Rotation = packet.Value;
        }

        private void HandleSetPropertyPacket(SetPropertyPacket packet)
        {
            if (packet.Id == _client.Id)
            {
                _client.SetProperty(packet.Key, packet.Value);
                return;
            }
            
            var entity = _world.GetEntity(packet.Id);
            entity?.SetProperty(packet.Key, packet.Value);
        }
    }
}