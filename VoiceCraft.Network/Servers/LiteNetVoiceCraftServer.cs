using System;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Systems;

namespace VoiceCraft.Network.Servers;

public class LiteNetVoiceCraftServer : VoiceCraftServer
{
    private readonly LiteNetListener _listener;
    private readonly NetManager _netManager;
    private readonly NetDataWriter _writer;
    
    public LiteNetVoiceCraftServer(VoiceCraftWorld world, AudioEffectSystem audioEffectSystem) : base(world,
        audioEffectSystem)
    {
        _listener = new LiteNetListener();
        _writer = new NetDataWriter();
        _netManager = new NetManager(_listener)
        {
            AutoRecycle = true,
            IPv6Enabled = false,
            UnconnectedMessagesEnabled = true
        };
        
        _listener.ConnectionRequest += OnConnectionRequest;
    }

    private void OnConnectionRequest(ConnectionRequest request)
    {
    }

    private class LiteNetListener : INetEventListener
    {
        // ReSharper disable InconsistentNaming
        public event Action<NetPeer>? PeerConnected;
        public event Action<NetPeer, DisconnectInfo>? PeerDisconnected;
        public event Action<IPEndPoint, SocketError>? NetworkError;
        public event Action<NetPeer, NetPacketReader, byte, DeliveryMethod>? NetworkReceive;
        public event Action<IPEndPoint, NetPacketReader, UnconnectedMessageType>? NetworkReceiveUnconnected;
        public event Action<NetPeer, int>? NetworkLatencyUpdate;
        public event Action<ConnectionRequest>? ConnectionRequest;
        
        public void OnPeerConnected(NetPeer peer)
        {
            PeerConnected?.Invoke(peer);
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            PeerDisconnected?.Invoke(peer, disconnectInfo);
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            NetworkError?.Invoke(endPoint, socketError);
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            NetworkReceive?.Invoke(peer, reader, channelNumber, deliveryMethod);
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            NetworkReceiveUnconnected?.Invoke(remoteEndPoint, reader, messageType);
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            NetworkLatencyUpdate?.Invoke(peer, latency);
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            ConnectionRequest?.Invoke(request);
        }
    }
}