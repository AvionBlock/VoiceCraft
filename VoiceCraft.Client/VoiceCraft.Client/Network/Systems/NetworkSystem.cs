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
        private readonly EventBasedNetListener _listener;
        private readonly VoiceCraftWorld _world;

        public event Action<ServerInfo>? OnServerInfo;
        public event Action<string>? OnSetTitle;

        public NetworkSystem(VoiceCraftClient client)
        {
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
                        HandleEntityCreatedPacket(entityCreatedPacket, reader);
                        break;
                    case PacketType.EntityReset:
                        var entityResetPacket = new EntityResetPacket();
                        entityResetPacket.Deserialize(reader);
                        HandleEntityResetPacket(entityResetPacket);
                        break;
                    case PacketType.EntityDestroyed:
                        var entityDestroyedPacket = new EntityDestroyedPacket();
                        entityDestroyedPacket.Deserialize(reader);
                        HandleEntityDestroyedPacket(entityDestroyedPacket);
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
                    case PacketType.RemoveProperty:
                        var removePropertyPacket = new RemovePropertyPacket();
                        removePropertyPacket.Deserialize(reader);
                        HandleRemovePropertyPacket(removePropertyPacket);
                        break;
                    case PacketType.Info:
                    case PacketType.Login:
                    case PacketType.SetEffect:
                    case PacketType.RemoveEffect:
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
                        HandleInfoPacket(infoPacket);
                        break;
                    //Unused
                    case PacketType.Login:
                    case PacketType.Audio:
                    case PacketType.SetTitle:
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

            reader.Recycle();
        }

        private void HandleInfoPacket(InfoPacket infoPacket)
        {
            OnServerInfo?.Invoke(new ServerInfo(infoPacket));
        }

        private void HandleAudioPacket(AudioPacket packet)
        {
            var entity = _world.GetEntity(packet.Id);
            entity?.ReceiveAudio(packet.Data, packet.Timestamp);
        }

        private void HandleSetTitlePacket(SetTitlePacket packet)
        {
            OnSetTitle?.Invoke(packet.Title);
        }

        private void HandleEntityCreatedPacket(EntityCreatedPacket packet, NetDataReader reader)
        {
            var entity = new VoiceCraftClientEntity(packet.Id);
            entity.Deserialize(reader);
            _world.AddEntity(entity);
        }

        private void HandleEntityResetPacket(EntityResetPacket packet)
        {
            var entity = _world.GetEntity(packet.Id);
            entity?.ResetProperties();
        }

        private void HandleEntityDestroyedPacket(EntityDestroyedPacket packet)
        {
            _world.DestroyEntity(packet.Id);
        }

        private void HandleSetNamePacket(SetNamePacket packet)
        {
            var entity = _world.GetEntity(packet.Id);
            if (entity == null) return;
            entity.Name = packet.Name;
        }

        private void HandleSetTalkBitmaskPacket(SetTalkBitmaskPacket packet)
        {
            var entity = _world.GetEntity(packet.Id);
            if (entity == null) return;
            entity.TalkBitmask = packet.Bitmask;
        }

        private void HandleSetListenBitmaskPacket(SetListenBitmaskPacket packet)
        {
            var entity = _world.GetEntity(packet.Id);
            if (entity == null) return;
            entity.ListenBitmask = packet.Bitmask;
        }

        private void HandleSetMinRangePacket(SetMinRangePacket packet)
        {
            var entity = _world.GetEntity(packet.Id);
            if (entity == null) return;
            entity.MinRange = packet.MinRange;
        }

        private void HandleSetMaxRangePacket(SetMaxRangePacket packet)
        {
            var entity = _world.GetEntity(packet.Id);
            if (entity == null) return;
            entity.MaxRange = packet.MaxRange;
        }

        private void HandleSetPositionPacket(SetPositionPacket packet)
        {
            var entity = _world.GetEntity(packet.Id);
            if (entity == null) return;
            entity.Position = packet.Position;
        }

        private void HandleSetRotationPacket(SetRotationPacket packet)
        {
            var entity = _world.GetEntity(packet.Id);
            if (entity == null) return;
            entity.Rotation = packet.Rotation;
        }

        private void HandleSetPropertyPacket(SetPropertyPacket packet)
        {
            var entity = _world.GetEntity(packet.Id);
            if (entity == null || packet.Value == null) return;
            entity.SetProperty(packet.Key, packet.Value);
        }

        private void HandleRemovePropertyPacket(RemovePropertyPacket packet)
        {
            var entity = _world.GetEntity(packet.Id);
            entity?.RemoveProperty(packet.Key);
        }
    }
}