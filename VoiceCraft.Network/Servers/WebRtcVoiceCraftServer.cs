using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Fleck;
using LiteNetLib.Utils;
using SIPSorcery.Net;
using VoiceCraft.Core;
using VoiceCraft.Core.JsonConverters;
using VoiceCraft.Core.World;
using VoiceCraft.Network.NetPeers;
using VoiceCraft.Network.Packets.VcPackets;
using VoiceCraft.Network.Packets.VcPackets.Request;
using VoiceCraft.Network.Packets.VcPackets.Response;
using VoiceCraft.Network.World;

namespace VoiceCraft.Network.Servers;

public class WebRtcVoiceCraftServer(VoiceCraftWorld world) : VoiceCraftServer(world)
{
    private LiteNetVoiceCraftServer.LiteNetVoiceCraftConfig _voiceCraftConfig = new();
    private WebRtcVoiceCraftConfig _config = new();
    private readonly ConcurrentDictionary<IWebSocketConnection, WebRtcSession> _sessions = new();
    private readonly ConcurrentDictionary<RTCDataChannel, WebRtcSession> _channels = new();
    private readonly NetDataReader _reader = new();
    private readonly NetDataWriter _writer = new();
    private readonly object _acceptLock = new();
    private WebSocketServer? _signalingServer;

    public WebRtcVoiceCraftConfig Config
    {
        get => _config;
        set
        {
            if (_signalingServer != null)
                throw new InvalidOperationException();
            _config = value;
        }
    }

    public LiteNetVoiceCraftServer.LiteNetVoiceCraftConfig VoiceCraftConfig
    {
        get => _voiceCraftConfig;
        set
        {
            if (_signalingServer != null)
                throw new InvalidOperationException();
            _voiceCraftConfig = value;
        }
    }

    public override PositioningType PositioningType => _voiceCraftConfig.PositioningType;
    public override string Motd => _voiceCraftConfig.Motd;
    public override uint MaxClients => _voiceCraftConfig.MaxClients;
    public override int ConnectedPeers => _sessions.Values.Count(x => x.Peer?.ConnectionState == VcConnectionState.Connected);

    public override void Start()
    {
        Stop();
        if (!Config.Enabled) return;

        _signalingServer = new WebSocketServer(_config.SignalingUrl);
        _signalingServer.Start(socket =>
        {
            socket.OnClose = () => OnSignalingClosed(socket);
            socket.OnMessage = message => OnSignalingMessage(socket, message);
        });
    }

    public override void Update()
    {
    }

    public override void Stop()
    {
        if (_signalingServer == null)
        {
            _sessions.Clear();
            _channels.Clear();
            return;
        }

        foreach (var session in _sessions.Values.ToArray())
            CloseSession(session, "VoiceCraft.DisconnectReason.Shutdown");

        _signalingServer.Dispose();
        _signalingServer = null;
        _sessions.Clear();
        _channels.Clear();
    }

    public override void SendUnconnectedPacket<T>(IPEndPoint endPoint, T packet)
    {
        PacketPool<T>.Return(packet);
    }

    public override void SendPacket<T>(
        VoiceCraftNetPeer vcNetPeer,
        T packet,
        VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable)
    {
        if (vcNetPeer is not WebRtcVoiceCraftNetPeer { ConnectionState: VcConnectionState.Connected } webRtcPeer)
        {
            PacketPool<T>.Return(packet);
            return;
        }

        try
        {
            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)packet.PacketType);
                _writer.Put(packet);
                webRtcPeer.DataChannel.send(_writer.CopyData());
            }
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    public override void Broadcast<T>(
        T packet,
        VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable,
        params VoiceCraftNetPeer?[] excludes)
    {
        try
        {
            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)packet.PacketType);
                _writer.Put(packet);
                var data = _writer.CopyData();
                foreach (var peer in _sessions.Values.Select(x => x.Peer).OfType<WebRtcVoiceCraftNetPeer>())
                {
                    if (peer.ConnectionState != VcConnectionState.Connected || excludes.Contains(peer))
                        continue;
                    peer.DataChannel.send(data);
                }
            }
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    public override void Disconnect(VoiceCraftNetPeer vcNetPeer, string reason, bool force = false)
    {
        if (vcNetPeer is not WebRtcVoiceCraftNetPeer webRtcPeer) return;
        if (!_channels.TryGetValue(webRtcPeer.DataChannel, out var session)) return;

        if (!force && webRtcPeer.ConnectionState == VcConnectionState.Connected)
        {
            SendPacket(webRtcPeer, PacketPool<VcLogoutRequestPacket>.GetPacket(() => new VcLogoutRequestPacket()).Set(reason));
        }

        CloseSession(session, reason);
    }

    public override void DisconnectAll(string? reason = null)
    {
        foreach (var session in _sessions.Values.ToArray())
            CloseSession(session, reason ?? "VoiceCraft.DisconnectReason.Shutdown");
    }

    protected override void AcceptRequest(VcLoginRequestPacket packet, object? data)
    {
        if (data is not WebRtcSession session || session.DataChannel == null) return;

        try
        {
            lock (_acceptLock)
            {
                if (ConnectedPeers >= MaxClients)
                    throw new InvalidOperationException("VoiceCraft.DisconnectReason.ServerFull");

                var peer = new WebRtcVoiceCraftNetPeer(
                    session.DataChannel,
                    packet.UserGuid,
                    packet.ServerUserGuid,
                    packet.Locale,
                    packet.PositioningType);
                session.Peer = peer;
                peer.Tag = new VoiceCraftNetworkEntity(peer, World.GetNextId());

                if (peer.Tag is not VoiceCraftNetworkEntity entity)
                    throw new InvalidOperationException("VoiceCraft.DisconnectReason.Error");

                World.AddEntity(entity);
                SendPacket(peer,
                    PacketPool<VcAcceptResponsePacket>.GetPacket(() => new VcAcceptResponsePacket())
                        .Set(packet.RequestId));
            }
        }
        catch (Exception ex)
        {
            RejectRequest(packet, string.IsNullOrWhiteSpace(ex.Message)
                ? "VoiceCraft.DisconnectReason.Error"
                : ex.Message, session);
        }
    }

    protected override void SendInfoResponsePacket(object? data, VcInfoResponsePacket packet)
    {
        if (data is not WebRtcSession { DataChannel: { } dataChannel })
        {
            PacketPool<VcInfoResponsePacket>.Return(packet);
            return;
        }

        try
        {
            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)packet.PacketType);
                _writer.Put(packet);
                dataChannel.send(_writer.CopyData());
            }
        }
        finally
        {
            PacketPool<VcInfoResponsePacket>.Return(packet);
        }
    }

    protected override void RejectRequest(VcLoginRequestPacket packet, string reason, object? data)
    {
        if (data is not WebRtcSession session || session.DataChannel == null) return;

        var responsePacket = PacketPool<VcDenyResponsePacket>.GetPacket(() => new VcDenyResponsePacket())
            .Set(packet.RequestId, reason);
        try
        {
            lock (_writer)
            {
                _writer.Reset();
                _writer.Put((byte)responsePacket.PacketType);
                _writer.Put(responsePacket);
                session.DataChannel.send(_writer.CopyData());
            }
        }
        finally
        {
            PacketPool<VcDenyResponsePacket>.Return(responsePacket);
            CloseSession(session, reason);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (Disposed) return;
        base.Dispose(disposing);
        if (!disposing) return;
        Stop();
    }

    private void OnSignalingMessage(IWebSocketConnection socket, string message)
    {
        try
        {
            var signalingMessage = JsonSerializer.Deserialize(message, WebRtcSignalingJsonContext.Default.WebRtcSignalingMessage);
            if (signalingMessage == null) return;

            switch (signalingMessage.Type)
            {
                case WebRtcSignalingMessageType.Offer:
                    _ = HandleOfferAsync(socket, signalingMessage);
                    break;
                case WebRtcSignalingMessageType.Candidate:
                    HandleCandidate(socket, signalingMessage);
                    break;
            }
        }
        catch
        {
            socket.Close();
        }
    }

    private async Task HandleOfferAsync(IWebSocketConnection socket, WebRtcSignalingMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Sdp))
        {
            socket.Close();
            return;
        }

        var peerConnection = new RTCPeerConnection();
        var session = new WebRtcSession(socket, peerConnection);
        if (!_sessions.TryAdd(socket, session))
        {
            peerConnection.close();
            socket.Close();
            return;
        }

        peerConnection.onicecandidate += candidate =>
        {
            if (candidate == null) return;
            SendSignaling(socket, new WebRtcSignalingMessage
            {
                Type = WebRtcSignalingMessageType.Candidate,
                Candidate = candidate.candidate,
                SdpMid = candidate.sdpMid,
                SdpMLineIndex = candidate.sdpMLineIndex
            });
        };
        peerConnection.ondatachannel += dataChannel => ConfigureDataChannel(session, dataChannel);
        peerConnection.onconnectionstatechange += state =>
        {
            if (state is RTCPeerConnectionState.closed or RTCPeerConnectionState.failed or RTCPeerConnectionState.disconnected)
                CloseSession(session, state.ToString());
        };

        var result = peerConnection.setRemoteDescription(new RTCSessionDescriptionInit
        {
            type = RTCSdpType.offer,
            sdp = message.Sdp
        });
        if (result != SetDescriptionResultEnum.OK)
        {
            CloseSession(session, result.ToString());
            return;
        }

        var answer = peerConnection.createAnswer(null);
        await peerConnection.setLocalDescription(answer);
        SendSignaling(socket, new WebRtcSignalingMessage
        {
            Type = WebRtcSignalingMessageType.Answer,
            Sdp = answer.sdp
        });
    }

    private void HandleCandidate(IWebSocketConnection socket, WebRtcSignalingMessage message)
    {
        if (!_sessions.TryGetValue(socket, out var session) ||
            string.IsNullOrWhiteSpace(message.Candidate))
            return;

        session.PeerConnection.addIceCandidate(new RTCIceCandidateInit
        {
            candidate = message.Candidate,
            sdpMid = message.SdpMid,
            sdpMLineIndex = message.SdpMLineIndex ?? 0
        });
    }

    private void ConfigureDataChannel(WebRtcSession session, RTCDataChannel dataChannel)
    {
        session.DataChannel = dataChannel;
        _channels.TryAdd(dataChannel, session);
        dataChannel.onmessage += (_, _, data) => OnDataChannelMessage(session, data);
        dataChannel.onclose += () => CloseSession(session, "VoiceCraft.DisconnectReason.ConnectionClosed");
    }

    private void OnDataChannelMessage(WebRtcSession session, byte[] data)
    {
        lock (_reader)
        {
            try
            {
                _reader.Clear();
                _reader.SetSource(data);
                ProcessPacket(_reader, packet =>
                {
                    var context = session.Peer == null ? (object)session : session.Peer;
                    ExecutePacket(packet, context);
                });
            }
            catch
            {
                CloseSession(session, "VoiceCraft.DisconnectReason.Error");
            }
        }
    }

    private void OnSignalingClosed(IWebSocketConnection socket)
    {
        if (!_sessions.TryGetValue(socket, out var session)) return;
        CloseSession(session, "VoiceCraft.DisconnectReason.ConnectionClosed");
    }

    private void CloseSession(WebRtcSession session, string reason)
    {
        if (!_sessions.TryRemove(session.SignalingSocket, out _))
            return;

        if (session.DataChannel != null)
            _channels.TryRemove(session.DataChannel, out _);

        if (session.Peer != null)
        {
            session.Peer.SetConnectionState(VcConnectionState.Disconnected);
            if (session.Peer.Tag is VoiceCraftNetworkEntity { Destroyed: false } entity)
                World.DestroyEntity(entity.Id);
        }

        try
        {
            session.DataChannel?.close();
        }
        catch
        {
            // Do nothing.
        }

        try
        {
            session.PeerConnection.close();
        }
        catch
        {
            // Do nothing.
        }

        try
        {
            if (session.SignalingSocket.IsAvailable)
                session.SignalingSocket.Close();
        }
        catch
        {
            // Do nothing.
        }
    }

    private static void SendSignaling(IWebSocketConnection socket, WebRtcSignalingMessage message)
    {
        if (!socket.IsAvailable) return;
        socket.Send(JsonSerializer.Serialize(message, WebRtcSignalingJsonContext.Default.WebRtcSignalingMessage));
    }

    private sealed class WebRtcSession(IWebSocketConnection signalingSocket, RTCPeerConnection peerConnection)
    {
        public IWebSocketConnection SignalingSocket { get; } = signalingSocket;
        public RTCPeerConnection PeerConnection { get; } = peerConnection;
        public RTCDataChannel? DataChannel { get; set; }
        public WebRtcVoiceCraftNetPeer? Peer { get; set; }
    }

    public class WebRtcVoiceCraftConfig
    {
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool Enabled { get; set; }

        public string SignalingUrl { get; set; } = "ws://127.0.0.1:9052/";
    }
}

public enum WebRtcSignalingMessageType
{
    Offer,
    Answer,
    Candidate
}

public sealed class WebRtcSignalingMessage
{
    public WebRtcSignalingMessageType Type { get; set; }
    public string? Sdp { get; set; }
    public string? Candidate { get; set; }
    public string? SdpMid { get; set; }
    public ushort? SdpMLineIndex { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(WebRtcSignalingMessage))]
public partial class WebRtcSignalingJsonContext : JsonSerializerContext;
