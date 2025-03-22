using System;
using System.Diagnostics;
using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Client.Network.Systems
{
    public class NetworkSystem
    {
        private readonly VoiceCraftClient _client;
        private readonly EventBasedNetListener _listener;
        private readonly NetDataWriter _dataWriter;
        private readonly NetManager _netManager;

        public event Action<IPEndPoint, ServerInfo>? OnServerInfo;

        public NetworkSystem(VoiceCraftClient client, NetManager netManager)
        {
            _client = client;
            _netManager = netManager;
            _listener = _client.Listener;
            _dataWriter = _client.DataWriter;

            _listener.ConnectionRequestEvent += OnConnectionRequestEvent;
            _listener.NetworkReceiveEvent += OnNetworkReceiveEvent;
            _listener.NetworkReceiveUnconnectedEvent += OnNetworkReceiveUnconnectedEvent;
        }

        public bool SendPacket<T>(T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered) where T : VoiceCraftPacket
        {
            if (_client.ServerPeer?.ConnectionState != ConnectionState.Connected) return false;

            _dataWriter.Reset();
            _dataWriter.Put((byte)packet.PacketType);
            packet.Serialize(_dataWriter);
            _client.ServerPeer.Send(_dataWriter, deliveryMethod);
            return true;
        }

        public bool SendUnconnectedPacket<T>(IPEndPoint remoteEndPoint, T packet) where T : VoiceCraftPacket
        {
            _dataWriter.Reset();
            _dataWriter.Put((byte)packet.PacketType);
            packet.Serialize(_dataWriter);
            return _netManager.SendUnconnectedMessage(_dataWriter, remoteEndPoint);
        }
        
        public bool SendUnconnectedPacket<T>(string ip, uint port, T packet) where T : VoiceCraftPacket
        {
            _dataWriter.Reset();
            _dataWriter.Put((byte)packet.PacketType);
            packet.Serialize(_dataWriter);
            return _netManager.SendUnconnectedMessage(_dataWriter, ip, (int)port);
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
                    case PacketType.Info:
                    case PacketType.Login:
                    case PacketType.EntityCreated:
                    case PacketType.EntityDestroyed:
                    case PacketType.UpdatePosition:
                    case PacketType.UpdateRotation:
                    case PacketType.UpdateTalkBitmask:
                    case PacketType.UpdateListenBitmask:
                    case PacketType.UpdateName:
                    case PacketType.AddEffect:
                    case PacketType.UpdateEffect:
                    case PacketType.RemoveEffect:
                    case PacketType.Unknown:
                    default:
                        break;
                }

                reader.Recycle();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
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
                        var serverInfoPacket = new InfoPacket();
                        serverInfoPacket.Deserialize(reader);
                        OnServerInfo?.Invoke(remoteendpoint, new ServerInfo(serverInfoPacket));
                        break;
                    //Unused
                    case PacketType.Login:
                    case PacketType.EntityCreated:
                    case PacketType.EntityDestroyed:
                    case PacketType.UpdatePosition:
                    case PacketType.UpdateRotation:
                    case PacketType.UpdateTalkBitmask:
                    case PacketType.UpdateListenBitmask:
                    case PacketType.UpdateName:
                    case PacketType.AddEffect:
                    case PacketType.UpdateEffect:
                    case PacketType.RemoveEffect:
                    case PacketType.Unknown:
                    default:
                        break;
                }

                reader.Recycle();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}