using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Network.NetPeers;
using VoiceCraft.Network.Packets.VcPackets;
using VoiceCraft.Network.Packets.VcPackets.Request;
using VoiceCraft.Network.Packets.VcPackets.Response;

namespace VoiceCraft.Network.Clients;

public class LiteNetVoiceCraftClient : VoiceCraftClient
{
    private readonly NetManager _netManager;
    private readonly LiteNetListener _listener;
    private readonly NetDataWriter _writer;
    private LiteNetVoiceCraftNetPeer? _netPeer;

    public override PositioningType PositioningType => _netPeer?.PositioningType ?? PositioningType.Server;
    public override event Action? OnConnected;
    public override event Action<string?>? OnDisconnected;

    public LiteNetVoiceCraftClient(Func<IAudioEncoder> encoderFactory, Func<IAudioDecoder> decoderFactory) : base(
        encoderFactory, decoderFactory)
    {
        _listener = new LiteNetListener();
        _netManager = new NetManager(_listener)
        {
            AutoRecycle = true,
            IPv6Enabled = false,
            UnconnectedMessagesEnabled = true
        };
        _writer = new NetDataWriter();

        _listener.PeerDisconnected += OnPeerDisconnected;
        _listener.NetworkReceive += NetworkReceive;
        _netManager.Start();
    }

    public override async Task<ServerInfo> PingAsync(string ip, int port, CancellationToken token = default)
    {
        var packet = PacketPool<VcInfoRequestPacket>.GetPacket().Set(Environment.TickCount);
        SendUnconnectedPacket(ip, port, packet);
        var response = await GetUnconnectedResponseAsync<VcInfoResponsePacket>(TimeSpan.FromSeconds(8), token);
        return new ServerInfo(response);
    }

    public override async Task ConnectAsync(string ip, int port, Guid userGuid, Guid serverUserGuid, string locale,
        PositioningType positioningType)
    {
        if (ConnectionState != VcConnectionState.Disconnected) return;
        ConnectionState = VcConnectionState.Connecting;
        Reset();
        var requestId = Guid.NewGuid();
        var packet = PacketPool<VcLoginRequestPacket>.GetPacket()
            .Set(requestId, userGuid, serverUserGuid, locale, Version, positioningType);
        try
        {
            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)packet.PacketType);
                _writer.Put(packet);
                var peer = _netManager.Connect(ip, port, _writer);
                _netPeer = new LiteNetVoiceCraftNetPeer(peer, Version, userGuid, serverUserGuid, locale,
                    positioningType);
            }

            _ = await GetResponseAsync<VcAcceptResponsePacket>(requestId, TimeSpan.FromSeconds(8));
            ConnectionState = VcConnectionState.Connected;
            OnConnected?.Invoke();
        }
        catch (Exception ex)
        {
            await DisconnectAsync(ex.Message);
        }
        finally
        {
            PacketPool<VcLoginRequestPacket>.Return(packet);
        }
    }

    public override void Update()
    {
        _netManager.PollEvents();
    }

    public override async Task DisconnectAsync(string? reason = null)
    {
        if (ConnectionState is not (VcConnectionState.Disconnected or VcConnectionState.Disconnecting)) return;
        ConnectionState = VcConnectionState.Disconnecting;
        _netManager.DisconnectAll();
        while (_netPeer?.ConnectionState == VcConnectionState.Disconnecting)
        {
            await Task.Delay(1);
        }

        ConnectionState = VcConnectionState.Disconnected;
        OnDisconnected?.Invoke(reason);
    }

    public override void SendUnconnectedPacket<T>(string ip, int port, T packet)
    {
        try
        {
            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)packet.PacketType);
                _netManager.SendUnconnectedMessage(_writer, ip, port);
            }
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    public override void SendPacket<T>(T packet, VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable)
    {
        try
        {
            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)packet.PacketType);
                _netPeer?.Send(_writer.AsReadOnlySpan(), deliveryMethod);
            }
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (Disposed) return;
        if (disposing)
        {
            _netManager.Stop();
            _listener.PeerDisconnected -= OnPeerDisconnected;
            _listener.NetworkReceive -= NetworkReceive;

            OnConnected = null;
            OnDisconnected = null;
        }

        base.Dispose(disposing);
    }

    private async Task<T> GetUnconnectedResponseAsync<T>(TimeSpan timeout, CancellationToken token = default)
        where T : IVoiceCraftPacket
    {
        var tcs = new TaskCompletionSource<T>();
        using var cts = new CancellationTokenSource(timeout);
        cts.Token.Register(() => tcs.TrySetException(new TimeoutException()));
        token.Register(() => tcs.TrySetException(new OperationCanceledException()));

        _listener.NetworkReceiveUnconnected += EventCallback;
        try
        {
            var result = await tcs.Task.ConfigureAwait(false);
            return result;
        }
        finally
        {
            _listener.NetworkReceiveUnconnected -= EventCallback;
        }

        void EventCallback(IPEndPoint _, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            ProcessUnconnectedPacket(reader, packet =>
            {
                if (packet is not T typedPacket) return;
                tcs.TrySetResult(typedPacket);
            });
        }
    }

    private async Task<T> GetResponseAsync<T>(Guid requestId, TimeSpan timeout, CancellationToken token = default)
        where T : IVoiceCraftPacket, IVoiceCraftRIdPacket
    {
        var tcs = new TaskCompletionSource<T>();
        var dTcs = new TaskCompletionSource<string?>();
        using var cts = new CancellationTokenSource(timeout);
        cts.Token.Register(() => tcs.TrySetException(new TimeoutException()));
        token.Register(() => tcs.TrySetException(new OperationCanceledException()));

        OnDisconnected += OnDisconnectedCallback;
        _listener.NetworkReceive += EventCallback;
        try
        {
            var result = await Task.WhenAny(tcs.Task, dTcs.Task).ConfigureAwait(false);
            return result == tcs.Task ? tcs.Task.Result : throw new Exception(dTcs.Task.Result);
        }
        finally
        {
            OnDisconnected -= OnDisconnectedCallback;
            _listener.NetworkReceive -= EventCallback;
        }

        void EventCallback(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            ProcessPacket(reader, packet =>
            {
                if (packet is not T typedPacket || typedPacket.RequestId != requestId) return;
                tcs.TrySetResult(typedPacket);
            });
        }

        void OnDisconnectedCallback(string? reason)
        {
            dTcs.TrySetResult(reason);
        }
    }

    #region LiteNetLib Events

    private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        if (peer.Equals(_netPeer?.NetPeer)) return;

        var disconnectReason = disconnectInfo.Reason.ToString();
        switch (disconnectInfo.Reason)
        {
            case DisconnectReason.ConnectionRejected:
            case DisconnectReason.RemoteConnectionClose:
            {
                if (disconnectInfo.AdditionalData.IsNull)
                    break;
                ProcessPacket(disconnectInfo.AdditionalData, packet =>
                {
                    disconnectReason = packet switch
                    {
                        VcDenyResponsePacket denyResponsePacket => denyResponsePacket.Reason,
                        VcLogoutRequestPacket logoutRequestPacket => logoutRequestPacket.Reason,
                        _ => disconnectReason
                    };
                });
            }
                break;
            case DisconnectReason.ConnectionFailed:
            case DisconnectReason.Timeout:
            case DisconnectReason.HostUnreachable:
            case DisconnectReason.NetworkUnreachable:
            case DisconnectReason.DisconnectPeerCalled:
            case DisconnectReason.InvalidProtocol:
            case DisconnectReason.UnknownHost:
            case DisconnectReason.Reconnect:
            case DisconnectReason.PeerToPeerConnection:
            case DisconnectReason.PeerNotFound:
            default:
                break;
        }

        _ = DisconnectAsync(disconnectReason);
    }

    private void NetworkReceive(NetPeer peer, NetPacketReader reader, byte channel,
        DeliveryMethod deliveryMethod)
    {
        ProcessPacket(reader, ExecutePacket);
    }

    #endregion

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