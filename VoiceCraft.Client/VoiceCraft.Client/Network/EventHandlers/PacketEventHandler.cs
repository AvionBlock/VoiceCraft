using System;
using LiteNetLib;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Client.Network.EventHandlers
{
    public class PacketEventHandler
    {
        private readonly VoiceCraftClient _client;
        public event Action<InfoPacket>? OnInfoPacketReceived;
        public event Action<AudioPacket>? OnAudioPacketReceived;
        public event Action<SetLocalEntityPacket>? OnSetLocalEntityPacketReceived;
        public event Action<EntityCreatedPacket>? OnEntityCreatedPacketReceived;
        public event Action<EntityDestroyedPacket>? OnEntityDestroyedPacketReceived;
        public event Action<AddComponentPacket>? OnAddComponentPacketReceived;
        public event Action<RemoveComponentPacket>? OnRemoveComponentPacketReceived;
        
        public PacketEventHandler(VoiceCraftClient client)
        {
            _client = client;
            
            _client.Listener.NetworkReceiveEvent += OnNetworkReceiveEvent;
        }
        
        private void OnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliverymethod)
        {
            var packetType = reader.GetByte();
            var pt = (PacketType)packetType;
            switch (pt)
            {
                case PacketType.Info:
                    var infoPacket = new InfoPacket();
                    infoPacket.Deserialize(reader);
                    OnInfoPacket(infoPacket);
                    break;
                case PacketType.Audio:
                    var audioPacket = new AudioPacket();
                    audioPacket.Deserialize(reader);
                    OnAudioPacket(audioPacket);
                    break;
                case PacketType.SetLocalEntity:
                    var localEntityPacket = new SetLocalEntityPacket();
                    localEntityPacket.Deserialize(reader);
                    OnSetLocalEntityPacket(localEntityPacket);
                    break;
                case PacketType.EntityCreated:
                    var entityCreatedPacket = new EntityCreatedPacket();
                    entityCreatedPacket.Deserialize(reader);
                    OnEntityCreatedPacket(entityCreatedPacket);
                    break;
                case PacketType.EntityDestroyed:
                    var entityDestroyedPacket = new EntityDestroyedPacket();
                    entityDestroyedPacket.Deserialize(reader);
                    OnEntityDestroyedPacket(entityDestroyedPacket);
                    break;
                case PacketType.AddComponent:
                    var addComponentPacket = new AddComponentPacket();
                    addComponentPacket.Deserialize(reader);
                    OnAddComponentPacket(addComponentPacket);
                    break;
                case PacketType.RemoveComponent:
                    var removeComponentPacket = new RemoveComponentPacket();
                    removeComponentPacket.Deserialize(reader);
                    OnRemoveComponentPacket(removeComponentPacket);
                    break;
                //Unused.
                case PacketType.Login:
                default:
                    break;
            }

            reader.Recycle();
        }

        private void OnInfoPacket(InfoPacket infoPacket)
        {
            OnInfoPacketReceived?.Invoke(infoPacket);
        }
        
        private void OnAudioPacket(AudioPacket audioPacket)
        {
            OnAudioPacketReceived?.Invoke(audioPacket);
        }

        private void OnSetLocalEntityPacket(SetLocalEntityPacket setLocalEntityPacket)
        {
            _client.LocalEntityId = setLocalEntityPacket.NetworkId;
            OnSetLocalEntityPacketReceived?.Invoke(setLocalEntityPacket);
        }

        private void OnEntityCreatedPacket(EntityCreatedPacket entityCreatedPacket)
        {
            OnEntityCreatedPacketReceived?.Invoke(entityCreatedPacket);
        }
        
        private void OnEntityDestroyedPacket(EntityDestroyedPacket entityDestroyedPacket)
        {
            OnEntityDestroyedPacketReceived?.Invoke(entityDestroyedPacket);
        }

        private void OnAddComponentPacket(AddComponentPacket addComponentPacket)
        {
            OnAddComponentPacketReceived?.Invoke(addComponentPacket);
        }
        
        private void OnRemoveComponentPacket(RemoveComponentPacket removeComponentPacket)
        {
            OnRemoveComponentPacketReceived?.Invoke(removeComponentPacket);
        }
    }
}