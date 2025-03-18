using System;
using System.Diagnostics;
using System.Net;
using LiteNetLib;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Client.Network.EventHandlers
{
    public class NetworkEventHandler
    {
        private readonly VoiceCraftClient _client;
        private readonly EventBasedNetListener _listener;
        private readonly NetManager _netManager;

        public NetworkEventHandler(VoiceCraftClient client, NetManager netManager)
        {
            _client = client;
            _netManager = netManager;
            _listener = _client.Listener;

            _listener.ConnectionRequestEvent += OnConnectionRequest;
            _listener.NetworkReceiveEvent += OnNetworkReceiveEvent;
            _listener.NetworkReceiveUnconnectedEvent += OnNetworkReceiveUnconnectedEvent;
        }

        private static void OnConnectionRequest(ConnectionRequest request)
        {
            request.Reject();
        }

        private void OnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliverymethod)
        {
            try
            {
                var packetType = reader.GetByte();
                var pt = (PacketType)packetType;
                switch (pt)
                {
                    case PacketType.EntityCreated:
                    case PacketType.EntityDestroyed:
                    //Unused
                    case PacketType.Login:
                    case PacketType.Info:
                    case PacketType.Unknown:
                    default:
                        break;
                }

                reader.Recycle();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
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
                        var packet = new InfoPacket();
                        packet.Deserialize(reader);
                        _client.ServerInfo = new ServerInfo(packet);
                        break;
                    //Unused
                    case PacketType.Login:
                    case PacketType.EntityCreated:
                    case PacketType.EntityDestroyed:
                    case PacketType.Unknown:
                    default:
                        break;
                }

                reader.Recycle();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}