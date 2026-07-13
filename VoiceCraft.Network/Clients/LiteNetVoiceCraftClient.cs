using System;
using System.Net;
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
    private readonly EventBasedNetListener _listener;
    private readonly NetDataWriter _writer;
    private NetManager? _netManager;
    private LiteNetVoiceCraftNetPeer? _netPeer;

    public override PositioningType PositioningType => _netPeer?.PositioningType ?? PositioningType.Server;
    public override event Action? OnConnected;
    public override event Action<string?>? OnDisconnected;

    public LiteNetVoiceCraftClient(IAudioEncoder audioEncoder, Func<IAudioDecoder> decoderFactory) : base(
        audioEncoder, decoderFactory)
    {
        _listener = new EventBasedNetListener();
        _writer = new NetDataWriter();

        _listener.ConnectionRequestEvent += ConnectionRequestEvent;
        _listener.PeerDisconnectedEvent += PeerDisconnectedEvent;
        _listener.NetworkReceiveUnconnectedEvent += NetworkReceiveUnconnectedEvent;
        _listener.NetworkReceiveEvent += NetworkReceiveEvent;
    }

    public override async Task<ServerInfo> PingAsync(string ip, int port, CancellationToken token = default)
    {
        if (_netManager == null)
            StartNetManager();

        var packet = PacketPool<VcInfoRequestPacket>.GetPacket(() => new VcInfoRequestPacket());
        try
        {
            packet.Set(Environment.TickCount);
            SendUnconnectedPacket(ip, port, packet);
            return await GetUnconnectedResponseAsync<VcInfoResponsePacket, ServerInfo>(
                response => new ServerInfo(response),
                TimeSpan.FromSeconds(8),
                token);
        }
        finally
        {
            packet.Return();
        }
    }

    public override async Task ConnectAsync(string ip, int port, Guid userGuid, Guid serverUserGuid, string locale,
        PositioningType positioningType)
    {
        if (ConnectionState != VcConnectionState.Disconnected) return;
        ConnectionState = VcConnectionState.Connecting;
        StopNetManager();
        StartNetManager();
        if (_netManager == null)
            throw new Exception(); //Should never happen.

        Reset();
        var requestId = Guid.NewGuid();
        var loginRequestPacket = PacketPool<VcLoginRequestPacket>.GetPacket(() => new VcLoginRequestPacket());
        try
        {
            loginRequestPacket.Set(requestId, userGuid, serverUserGuid, locale, Version, positioningType);
            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)loginRequestPacket.PacketType);
                _writer.Put(loginRequestPacket);
                var peer = _netManager.Connect(ip, port, _writer);
                _netPeer = new LiteNetVoiceCraftNetPeer(null, peer, userGuid, serverUserGuid, locale, positioningType);
            }

            _ = await GetResponseAsync<VcAcceptResponsePacket, Guid>(
                requestId,
                response => response.RequestId,
                TimeSpan.FromSeconds(8));
            ConnectionState = VcConnectionState.Connected;
            OnConnected?.Invoke();
        }
        catch (Exception ex)
        {
            await DisconnectAsync(ex.Message);
        }
        finally
        {
            loginRequestPacket.Return();
        }
    }

    public override void Update()
    {
        _netManager?.PollEvents();
    }

    public override async Task DisconnectAsync(string? reason = null)
    {
        if (ConnectionState is VcConnectionState.Disconnected or VcConnectionState.Disconnecting) return;
        ConnectionState = VcConnectionState.Disconnecting;
        if (_netPeer != null)
        {
            _netManager?.DisconnectPeer(_netPeer.NetPeer);
        }

        var disconnectDeadline = Environment.TickCount64 + 2_000;
        while (_netPeer?.ConnectionState == VcConnectionState.Disconnecting &&
               Environment.TickCount64 < disconnectDeadline)
        {
            await Task.Delay(10).ConfigureAwait(false);
        }

        if (_netPeer?.ConnectionState == VcConnectionState.Disconnecting)
            StopNetManager();

        _netPeer = null;
        ConnectionState = VcConnectionState.Disconnected;
        OnDisconnected?.Invoke(reason);
    }

    public override void SendUnconnectedPacket<T>(string ip, int port, T packet)
    {
        lock (_writer)
        {
            _writer.Reset();
            _writer.Put((byte)packet.PacketType);
            _writer.Put(packet);
            _netManager?.SendUnconnectedMessage(_writer, ip, port);
        }
    }

    public override void SendPacket<T>(T packet, VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable)
    {
        if (_netPeer == null) return;
        var method = deliveryMethod switch
        {
            VcDeliveryMethod.Unreliable => DeliveryMethod.Unreliable,
            _ => DeliveryMethod.ReliableOrdered
        };
        lock (_writer)
        {
            _writer.Reset();
            _writer.Put((byte)packet.PacketType);
            _writer.Put(packet);
            _netPeer.NetPeer.Send(_writer, method);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (Disposed) return;
        if (disposing)
        {
            _netManager?.Stop();
            _listener.ConnectionRequestEvent -= ConnectionRequestEvent;
            _listener.PeerDisconnectedEvent -= PeerDisconnectedEvent;
            _listener.NetworkReceiveUnconnectedEvent -= NetworkReceiveUnconnectedEvent;
            _listener.NetworkReceiveEvent -= NetworkReceiveEvent;

            OnConnected = null;
            OnDisconnected = null;
        }

        base.Dispose(disposing);
    }

    private void StartNetManager()
    {
        _netManager = new NetManager(_listener)
        {
            AutoRecycle = true,
            IPv6Enabled = false,
            UnconnectedMessagesEnabled = true
        };
        _netManager.Start();
    }

    private void StopNetManager()
    {
        _netManager?.Stop();
        _netManager = null;
    }

    private async Task<TResult> GetUnconnectedResponseAsync<TPacket, TResult>(
        Func<TPacket, TResult> selector,
        TimeSpan timeout,
        CancellationToken token = default)
        where TPacket : IVoiceCraftPacket
    {
        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cts = new CancellationTokenSource(timeout);
        using var timeoutRegistration = cts.Token.Register(() => tcs.TrySetException(new TimeoutException()));
        using var cancellationRegistration = token.Register(() => tcs.TrySetCanceled(token));

        OnUnconnectedPacketReceived += EventCallback;
        try
        {
            var result = await tcs.Task.ConfigureAwait(false);
            return result;
        }
        finally
        {
            OnUnconnectedPacketReceived -= EventCallback;
        }

        void EventCallback(IVoiceCraftPacket packet)
        {
            if (packet is not TPacket typedPacket) return;
            try
            {
                tcs.TrySetResult(selector(typedPacket));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }
    }

    private async Task<TResult> GetResponseAsync<TPacket, TResult>(
        Guid requestId,
        Func<TPacket, TResult> selector,
        TimeSpan timeout,
        CancellationToken token = default)
        where TPacket : IVoiceCraftPacket, IVoiceCraftRIdPacket
    {
        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var dTcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cts = new CancellationTokenSource(timeout);
        using var timeoutRegistration = cts.Token.Register(() => tcs.TrySetException(new TimeoutException()));
        using var cancellationRegistration = token.Register(() => tcs.TrySetCanceled(token));

        OnDisconnected += OnDisconnectedCallback;
        OnPacketReceived += EventCallback;
        try
        {
            var completedTask = await Task.WhenAny(tcs.Task, dTcs.Task).ConfigureAwait(false);
            if (completedTask == tcs.Task)
                return await tcs.Task.ConfigureAwait(false);

            throw new Exception(await dTcs.Task.ConfigureAwait(false));
        }
        finally
        {
            OnDisconnected -= OnDisconnectedCallback;
            OnPacketReceived -= EventCallback;
        }

        void EventCallback(IVoiceCraftPacket packet)
        {
            if (packet is not TPacket typedPacket || typedPacket.RequestId != requestId) return;
            try
            {
                tcs.TrySetResult(selector(typedPacket));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }

        void OnDisconnectedCallback(string? reason)
        {
            dTcs.TrySetResult(reason);
        }
    }

    #region LiteNetLib Events

    private static void ConnectionRequestEvent(ConnectionRequest request)
    {
        //No Fuck you.
        request.Reject();
    }

    private void PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        if (!peer.Equals(_netPeer?.NetPeer)) return;
        _netPeer = null;

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

    private void NetworkReceiveUnconnectedEvent(IPEndPoint remoteEndPoint, NetPacketReader reader,
        UnconnectedMessageType messageType)
    {
        try
        {
            ProcessUnconnectedPacket(reader, _ => { }); //Do nothing with it.
        }
        catch
        {
            //Do Nothing
        }
    }

    private void NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel,
        DeliveryMethod deliveryMethod)
    {
        try
        {
            ProcessPacket(reader, ExecutePacket);
        }
        catch
        {
            // Do nothing
        }
    }

    #endregion
}
