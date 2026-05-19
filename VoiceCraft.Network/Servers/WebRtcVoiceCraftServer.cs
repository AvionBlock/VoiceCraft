using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Fleck;
using LiteNetLib.Utils;
using SIPSorcery.Net;
using SIPSorcery.Sys;
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
    private IReadOnlyDictionary<int, WebRtcExternalIceCandidateMapping> _externalIceCandidateMappings =
        new Dictionary<int, WebRtcExternalIceCandidateMapping>();
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

    public IReadOnlyCollection<WebRtcExternalIceCandidateMapping> ExternalIceCandidateMappings
    {
        get => _externalIceCandidateMappings.Values.ToArray();
        set => _externalIceCandidateMappings = (value ?? [])
            .Where(x => x.InternalPort is >= 1 and <= 65535 &&
                        x.ExternalPort is >= 1 and <= 65535 &&
                        !string.IsNullOrWhiteSpace(x.ExternalAddress))
            .GroupBy(x => x.InternalPort)
            .ToDictionary(x => x.Key, x => x.First());
    }

    public override PositioningType PositioningType => _voiceCraftConfig.PositioningType;
    public override string Motd => _voiceCraftConfig.Motd;
    public override uint MaxClients => _voiceCraftConfig.MaxClients;
    public override int ConnectedPeers => _sessions.Values.Count(x => x.Peer?.ConnectionState == VcConnectionState.Connected);

    public override void Start()
    {
        Stop();
        if (!Config.Enabled) return;

        _signalingServer = CreateSignalingServer();
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

        var peerConnection = CreatePeerConnection();
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
            SendIceCandidate(socket, candidate.candidate, candidate.sdpMid, candidate.sdpMLineIndex);
            foreach (var mappedCandidate in GetMappedIceCandidates(candidate.candidate))
            {
                AddLocalMappedIceCandidate(peerConnection, mappedCandidate.Mapping);
                SendIceCandidate(socket, mappedCandidate.Candidate, candidate.sdpMid, candidate.sdpMLineIndex);
            }
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

    private static void SendIceCandidate(
        IWebSocketConnection socket,
        string candidate,
        string? sdpMid,
        ushort? sdpMLineIndex)
    {
        SendSignaling(socket, new WebRtcSignalingMessage
        {
            Type = WebRtcSignalingMessageType.Candidate,
            Candidate = candidate,
            SdpMid = sdpMid,
            SdpMLineIndex = sdpMLineIndex
        });
    }

    private RTCPeerConnection CreatePeerConnection()
    {
        return new RTCPeerConnection(CreateRtcConfiguration(), 0, CreateRtcPortRange(), false);
    }

    private WebSocketServer CreateSignalingServer()
    {
        var server = new WebSocketServer(_config.SignalingUrl);
        if (!IsSecureSignalingUrl(_config.SignalingUrl))
            return server;

        server.Certificate = WebRtcSignalingCertificateProvider.LoadOrCreate(_config.SignalingUrl, _config.Tls ?? new());
        server.EnabledSslProtocols = SslProtocols.None;
        return server;
    }

    private RTCConfiguration CreateRtcConfiguration()
    {
        var configuration = new RTCConfiguration
        {
            iceServers = (Config.IceServers ?? [])
                .Where(x => !string.IsNullOrWhiteSpace(x.Urls))
                .Select(x => new RTCIceServer
                {
                    urls = x.Urls,
                    username = x.Username,
                    credential = x.Credential
                })
                .ToList(),
            X_GatherTimeoutMs = Config.IceGatherTimeoutMs
        };

        if (IPAddress.TryParse(Config.BindAddress, out var bindAddress))
            configuration.X_BindAddress = bindAddress;

        return configuration;
    }

    private PortRange? CreateRtcPortRange()
    {
        if (Config.PortRangeStart == null && Config.PortRangeEnd == null)
            return null;

        if (Config.PortRangeStart is not >= 1 or > 65535 ||
            Config.PortRangeEnd is not >= 1 or > 65535 ||
            Config.PortRangeEnd < Config.PortRangeStart)
            throw new InvalidOperationException(
                "WebRTC PortRangeStart and PortRangeEnd must be valid ports, and PortRangeEnd must be greater than or equal to PortRangeStart.");

        return new PortRange(Config.PortRangeStart.Value, Config.PortRangeEnd.Value, false, null);
    }

    private static bool IsSecureSignalingUrl(string signalingUrl) =>
        Uri.TryCreate(signalingUrl, UriKind.Absolute, out var uri) &&
        string.Equals(uri.Scheme, "wss", StringComparison.OrdinalIgnoreCase);

    private static void AddLocalMappedIceCandidate(
        RTCPeerConnection peerConnection,
        WebRtcExternalIceCandidateMapping mapping)
    {
        try
        {
            if (IPAddress.TryParse(mapping.ExternalAddress, out var address))
            {
                peerConnection.addLocalIceCandidate(new RTCIceCandidate(
                    RTCIceProtocol.udp,
                    address,
                    (ushort)mapping.ExternalPort,
                    RTCIceCandidateType.host));
            }
        }
        catch
        {
            // The mapped candidate is still sent to the browser; the local socket can receive the mapped traffic.
        }
    }

    private IEnumerable<MappedIceCandidate> GetMappedIceCandidates(string candidate)
    {
        if (_externalIceCandidateMappings.Count == 0 ||
            string.IsNullOrWhiteSpace(candidate))
            yield break;

        var parts = candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 8 ||
            !string.Equals(parts[2], "udp", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(parts[6], "typ", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(parts[7], "host", StringComparison.OrdinalIgnoreCase) ||
            !int.TryParse(parts[5], NumberStyles.None, CultureInfo.InvariantCulture, out var internalPort) ||
            !_externalIceCandidateMappings.TryGetValue(internalPort, out var mapping))
            yield break;

        if (string.Equals(parts[4], mapping.ExternalAddress, StringComparison.OrdinalIgnoreCase) &&
            internalPort == mapping.ExternalPort)
            yield break;

        parts[4] = mapping.ExternalAddress;
        parts[5] = mapping.ExternalPort.ToString(CultureInfo.InvariantCulture);
        yield return new MappedIceCandidate(string.Join(' ', parts), mapping);
    }

    private readonly record struct MappedIceCandidate(
        string Candidate,
        WebRtcExternalIceCandidateMapping Mapping);

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
        public List<WebRtcIceServerConfig> IceServers { get; set; } =
        [
            new() { Urls = "stun:stun.l.google.com:19302" },
            new() { Urls = "stun:stun1.l.google.com:19302" },
            new() { Urls = "stun:stun2.l.google.com:19302" },
            new() { Urls = "stun:stun.cloudflare.com:3478" }
        ];
        public string? BindAddress { get; set; }
        public int IceGatherTimeoutMs { get; set; } = 5000;
        public int? PortRangeStart { get; set; } = 9053;
        public int? PortRangeEnd { get; set; } = 9062;
        public WebRtcTlsConfig Tls { get; set; } = new();
        public WebRtcPortMappingConfig PortMapping { get; set; } = new();
    }

    public class WebRtcIceServerConfig
    {
        public string Urls { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? Credential { get; set; }
    }

    public class WebRtcTlsConfig
    {
        public string CertificateMode { get; set; } = WebRtcCertificateModes.LetsEncrypt;
        public bool AutoGenerateCertificate { get; set; } = true;
        public string CertificatePath { get; set; } = "config/webrtc-signaling.pfx";
        public string? CertificatePassword { get; set; }
        public string? SubjectName { get; set; }
        public List<string> SubjectAlternativeNames { get; set; } = [];
        public int CertificateLifetimeDays { get; set; } = 3650;
        public WebRtcAcmeConfig Acme { get; set; } = new();
    }

    public class WebRtcAcmeConfig
    {
        public string DirectoryUrl { get; set; } = "https://acme-v02.api.letsencrypt.org/directory";
        public string StagingDirectoryUrl { get; set; } = "https://acme-staging-v02.api.letsencrypt.org/directory";
        public bool UseStaging { get; set; }
        public string? Email { get; set; }
        public List<string> Domains { get; set; } = [];
        public string AccountKeyPath { get; set; } = "config/acme-account.key";
        public int HttpChallengePort { get; set; } = 80;
        public string HttpChallengeBindAddress { get; set; } = "0.0.0.0";
        public bool AutoMapHttpChallengePort { get; set; } = true;
        public int ValidationTimeoutSeconds { get; set; } = 120;
        public int RenewBeforeDays { get; set; } = 30;
    }

    public static class WebRtcCertificateModes
    {
        public const string LetsEncrypt = "lets-encrypt";
        public const string SelfSigned = "self-signed";
        public const string Existing = "existing";
    }

    public class WebRtcPortMappingConfig
    {
        public bool Enabled { get; set; }
        public bool MapSignalingPort { get; set; } = true;
        public bool MapUdpPortRange { get; set; } = true;
        public bool FailOnFailure { get; set; }
        public string? PublicAddress { get; set; }
        public int LifetimeMinutes { get; set; } = 60;
        public int TimeoutMs { get; set; } = 5000;
    }

    public class WebRtcExternalIceCandidateMapping
    {
        public int InternalPort { get; set; }
        public string ExternalAddress { get; set; } = string.Empty;
        public int ExternalPort { get; set; }
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
