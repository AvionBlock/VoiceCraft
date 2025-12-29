using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using OpusSharp.Core;
using OpusSharp.Core.Extensions;
using VoiceCraft.Client.Network.Systems;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Network.VcPackets;
using VoiceCraft.Core.Network.VcPackets.Event;
using VoiceCraft.Core.Network.VcPackets.Request;
using VoiceCraft.Core.Network.VcPackets.Response;
using VoiceCraft.Core.World;

namespace VoiceCraft.Client.Network;

public class VoiceCraftClient : VoiceCraftEntity, IDisposable, INetEventListener
{
    public static readonly Version Version = new(Constants.Major, Constants.Minor, 0);

    //Systems
    private readonly AudioSystem _audioSystem;

    //Buffers
    private readonly NetDataWriter _dataWriter = new();
    private readonly byte[] _encodeBuffer = new byte[Constants.MaximumEncodedBytes];

    //Encoder
    private readonly OpusEncoder _encoder;
    private readonly NetManager _netManager;
    private readonly HashSet<Guid> _requestIds = [];

    //Networking
    private bool _isDisposed;
    private DateTime _lastAudioPeakTime = DateTime.MinValue;
    private ushort _sendTimestamp;

    //Privates
    private NetPeer? _serverPeer;
    private bool _speakingState;
    private PositioningType _positioningType;

    public VoiceCraftClient() : base(0, new VoiceCraftWorld())
    {
        _netManager = new NetManager(this)
        {
            AutoRecycle = true,
            IPv6Enabled = false,
            UnconnectedMessagesEnabled = true
        };

        _encoder = new OpusEncoder(Constants.SampleRate, Constants.Channels,
            OpusPredefinedValues.OPUS_APPLICATION_VOIP);
        _encoder.SetPacketLostPercent(20); //Expected packet loss, might make this change over time later.
        _encoder.SetBitRate(32000);

        //Setup Systems.
        _audioSystem = new AudioSystem(this, World);

        //Internal Listeners
        OnWorldIdUpdated += OnClientWorldIdUpdated;
        OnNameUpdated += OnClientNameUpdated;
        OnMuteUpdated += OnClientMuteUpdated;
        OnDeafenUpdated += OnClientDeafenUpdated;
        OnPositionUpdated += OnClientPositionUpdated;
        OnRotationUpdated += OnClientRotationUpdated;
        OnCaveFactorUpdated += OnClientCaveFactorUpdated;
        OnMuffleFactorUpdated += OnClientMuffleFactorUpdated;

        //Start
        _netManager.Start();
    }

    //Public Properties
    public VcConnectionState ConnectionState { get; private set; } = VcConnectionState.Disconnected;

    public float MicrophoneSensitivity { get; set; }
    public float OutputVolume { get; set; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    //Events
    public event Action? OnConnected;
    public event Action<string>? OnDisconnected;
    public event Action<ServerInfo>? OnServerInfo;
    public event Action<string>? OnSetTitle;
    public event Action<string>? OnSetDescription;
    public event Action<bool>? OnSpeakingUpdated;
    public event Action<IVoiceCraftPacket>? OnPacket;

    ~VoiceCraftClient()
    {
        Dispose(false);
    }

    public bool Ping(string ip, uint port)
    {
        var packet = PacketPool<VcInfoRequestPacket>.GetPacket().Set(Environment.TickCount);
        var endPoint = NetUtils.MakeEndPoint(ip, (int)port);
        try
        {
            SendUnconnectedPacket(endPoint, packet);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task ConnectAsync(
        Guid userGuid,
        Guid serverUserGuid,
        string ip,
        int port,
        string locale,
        PositioningType positioningType)
    {
        ThrowIfDisposed();
        if (ConnectionState != VcConnectionState.Disconnected)
            throw new InvalidOperationException("This client is already connected or is connecting to a server!");

        _speakingState = false;
        _requestIds.Clear();
        _positioningType = positioningType;

        var loginPacket = PacketPool<VcLoginRequestPacket>.GetPacket();
        loginPacket.Set(Guid.NewGuid(), userGuid, serverUserGuid, locale, Version, positioningType);
        try
        {
            lock (_dataWriter)
            {
                _dataWriter.Reset();
                loginPacket.Serialize(_dataWriter);
                ConnectionState = VcConnectionState.Connecting;
                _serverPeer = _netManager.Connect(ip, port, _dataWriter) ??
                              throw new InvalidOperationException("A connection request is awaiting!");
            }

            _ = await GetResponseAsync<VcAcceptResponsePacket>(loginPacket.RequestId,
                TimeSpan.FromSeconds(8));
            ConnectionState = VcConnectionState.Connected;
            OnConnected?.Invoke();
        }
        catch
        {
            Disconnect("VoiceCraft.DisconnectReason.Error");
        }
        finally
        {
            PacketPool<VcLoginRequestPacket>.Return(loginPacket);
        }
    }

    public void Disconnect(string? reason = null)
    {
        if (_isDisposed || ConnectionState == VcConnectionState.Disconnected) return;
        ConnectionState = VcConnectionState.Disconnecting;
        _netManager.DisconnectAll();
    }

    public void Update()
    {
        _netManager.PollEvents();
        switch (_speakingState)
        {
            case false when
                (DateTime.UtcNow - _lastAudioPeakTime).TotalMilliseconds <= Constants.SilenceThresholdMs:
                _speakingState = true;
                OnSpeakingUpdated?.Invoke(true);
                break;
            case true when
                (DateTime.UtcNow - _lastAudioPeakTime).TotalMilliseconds > Constants.SilenceThresholdMs:
                _speakingState = false;
                OnSpeakingUpdated?.Invoke(false);
                break;
        }
    }

    public int Read(byte[] buffer, int count)
    {
        var bufferShort = MemoryMarshal.Cast<byte, short>(buffer);
        var read = _audioSystem.Read(bufferShort, count / sizeof(short)) * sizeof(short);
        return read;
    }

    public void Write(byte[] buffer, int bytesRead)
    {
        var frameLoudness = buffer.GetFramePeak16(bytesRead);
        if (frameLoudness >= MicrophoneSensitivity)
            _lastAudioPeakTime = DateTime.UtcNow;

        _sendTimestamp += 1; //Add to timestamp even though we aren't really connected.
        if ((DateTime.UtcNow - _lastAudioPeakTime).TotalMilliseconds > Constants.SilenceThresholdMs ||
            _serverPeer == null ||
            ConnectionState != VcConnectionState.Connected || Muted) return;

        Array.Clear(_encodeBuffer);
        var bytesEncoded = _encoder.Encode(buffer, Constants.SamplesPerFrame, _encodeBuffer, _encodeBuffer.Length);
        SendPacket(PacketPool<VcAudioRequestPacket>.GetPacket()
            .Set(_sendTimestamp, frameLoudness, bytesEncoded, _encodeBuffer));
    }

    public bool SendPacket<T>(T packet,
        DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered) where T : IVoiceCraftPacket
    {
        if (ConnectionState != VcConnectionState.Connected) return false;
        try
        {
            lock (_dataWriter)
            {
                _dataWriter.Reset();
                _dataWriter.Put((byte)packet.PacketType);
                packet.Serialize(_dataWriter);
                _serverPeer?.Send(_dataWriter, deliveryMethod);
                return true;
            }
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    public bool SendUnconnectedPacket<T>(IPEndPoint remoteEndPoint, T packet) where T : IVoiceCraftPacket
    {
        try
        {
            lock (_dataWriter)
            {
                _dataWriter.Reset();
                _dataWriter.Put((byte)packet.PacketType);
                packet.Serialize(_dataWriter);
                return _netManager.SendUnconnectedMessage(_dataWriter, remoteEndPoint);
            }
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    public async Task<T> GetResponseAsync<T>(Guid requestId, TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource<IVoiceCraftPacket>();
        using var cts = new CancellationTokenSource(timeout);
        cts.Token.Register(() => tcs.TrySetException(new TimeoutException()));

        OnPacket += EventCallback;
        try
        {
            if (!_requestIds.Add(requestId))
                throw new TaskCanceledException("A request with the same id already exists!");
            var result = await tcs.Task.ConfigureAwait(false);
            return result is T typedResult ? typedResult : throw new InvalidCastException();
        }
        finally
        {
            _requestIds.Remove(requestId);
            OnPacket -= EventCallback;
        }

        void EventCallback(IVoiceCraftPacket packet)
        {
            if (packet is not IVoiceCraftRIdPacket rIdPacket || rIdPacket.RequestId != requestId) return;
            tcs.TrySetResult(packet);
        }
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        if (disposing)
        {
            _netManager.Stop();
            _encoder.Dispose();
            World.Dispose();
            
            OnWorldIdUpdated -= OnClientWorldIdUpdated;
            OnNameUpdated -= OnClientNameUpdated;
            OnMuteUpdated -= OnClientMuteUpdated;
            OnDeafenUpdated -= OnClientDeafenUpdated;
            OnPositionUpdated -= OnClientPositionUpdated;
            OnRotationUpdated -= OnClientRotationUpdated;
            OnCaveFactorUpdated -= OnClientCaveFactorUpdated;
            OnMuffleFactorUpdated -= OnClientMuffleFactorUpdated;

            OnConnected = null;
            OnDisconnected = null;
            OnServerInfo = null;
            OnSetTitle = null;
            OnSetDescription = null;
            OnSpeakingUpdated = null;
        }

        _isDisposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (!_isDisposed) return;
        throw new ObjectDisposedException(typeof(VoiceCraftClient).ToString());
    }
    
    //Network Events
    public void OnPeerConnected(NetPeer peer)
    {
        //Do nothing.
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info)
    {
        if (!Equals(peer, _serverPeer)) return;
        try
        {
            World.ClearEntities();
            VcPacketType packetId;
            switch (info.Reason)
            {
                case DisconnectReason.ConnectionRejected when !info.AdditionalData.IsNull:
                    packetId = (VcPacketType)info.AdditionalData.GetByte();
                    if (packetId != VcPacketType.DenyResponse) break;
                    var denyPacket = PacketPool<VcDenyResponsePacket>.GetPacket();
                    denyPacket.Deserialize(info.AdditionalData);
                    OnDisconnected?.Invoke(denyPacket.Reason);
                    return;
                case DisconnectReason.RemoteConnectionClose when !info.AdditionalData.IsNull:
                    packetId = (VcPacketType)info.AdditionalData.GetByte();
                    if (packetId != VcPacketType.LogoutRequest) break;
                    var logoutPacket = PacketPool<VcLogoutRequestPacket>.GetPacket();
                    logoutPacket.Deserialize(info.AdditionalData);
                    OnDisconnected?.Invoke(logoutPacket.Reason);
                    return;
                case DisconnectReason.DisconnectPeerCalled:
                    OnDisconnected?.Invoke("VoiceCraft.DisconnectReason.Manual");
                    return;
            }

            OnDisconnected?.Invoke(info.Reason.ToString());
        }
        catch
        {
            OnDisconnected?.Invoke(info.Reason.ToString());
        }
        finally
        {
            ConnectionState = VcConnectionState.Disconnected;
        }
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        //Do nothing.
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        try
        {
            var packetType = reader.GetByte();
            var pt = (VcPacketType)packetType;
            ProcessPacket(pt, reader);
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
        }

        reader.Recycle();
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        try
        {
            var packetType = reader.GetByte();
            var pt = (VcPacketType)packetType;
            ProcessUnconnectedPacket(pt, reader);
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
        }

        reader.Recycle();
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        //Do nothing
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        request.Reject(); //No fuck you.
    }

    //Internal Event Handling
    private void OnClientWorldIdUpdated(string worldId, VoiceCraftEntity _)
    {
        if (_positioningType != PositioningType.Client) return;
        SendPacket(PacketPool<VcSetWorldIdRequestPacket>.GetPacket().Set(worldId));
    }

    private void OnClientNameUpdated(string name, VoiceCraftEntity _)
    {
        if (_positioningType != PositioningType.Client) return;
        SendPacket(PacketPool<VcSetNameRequestPacket>.GetPacket().Set(name));
    }

    private void OnClientMuteUpdated(bool value, VoiceCraftEntity _)
    {
        SendPacket(PacketPool<VcSetMuteRequestPacket>.GetPacket().Set(value));
    }

    private void OnClientDeafenUpdated(bool value, VoiceCraftEntity _)
    {
        SendPacket(PacketPool<VcSetDeafenRequestPacket>.GetPacket().Set(value));
    }

    private void OnClientPositionUpdated(Vector3 position, VoiceCraftEntity _)
    {
        if (_positioningType != PositioningType.Client) return;
        SendPacket(PacketPool<VcSetPositionRequestPacket>.GetPacket().Set(position));
    }
    
    private void OnClientRotationUpdated(Vector2 rotation, VoiceCraftEntity _)
    {
        if (_positioningType != PositioningType.Client) return;
        SendPacket(PacketPool<VcSetRotationRequestPacket>.GetPacket().Set(rotation));
    }
    
    private void OnClientCaveFactorUpdated(float caveFactor, VoiceCraftEntity _)
    {
        if (_positioningType != PositioningType.Client) return;
        SendPacket(PacketPool<VcSetCaveFactorRequest>.GetPacket().Set(caveFactor));
    }
    
    private void OnClientMuffleFactorUpdated(float muffleFactor, VoiceCraftEntity _)
    {
        if (_positioningType != PositioningType.Client) return;
        SendPacket(PacketPool<VcSetMuffleFactorRequest>.GetPacket().Set(muffleFactor));
    }

    //Packet Handling
    private void ProcessPacket(VcPacketType packetType, NetPacketReader reader)
    {
        switch (packetType)
        {
            //Requests
            case VcPacketType.InfoRequest:
                var infoRequestPacket = PacketPool<VcInfoRequestPacket>.GetPacket();
                infoRequestPacket.Deserialize(reader);
                HandleInfoRequestPacket(infoRequestPacket);
                break;
            case VcPacketType.LoginRequest:
                var loginRequestPacket = PacketPool<VcLoginRequestPacket>.GetPacket();
                loginRequestPacket.Deserialize(reader);
                HandleLoginRequestPacket(loginRequestPacket);
                break;
            case VcPacketType.LogoutRequest:
                var logoutRequestPacket = PacketPool<VcLogoutRequestPacket>.GetPacket();
                logoutRequestPacket.Deserialize(reader);
                HandleLogoutRequestPacket(logoutRequestPacket);
                break;
            case VcPacketType.SetNameRequest:
                var setNameRequestPacket = PacketPool<VcSetNameRequestPacket>.GetPacket();
                setNameRequestPacket.Deserialize(reader);
                HandleSetNameRequestPacket(setNameRequestPacket);
                break;
            case VcPacketType.AudioRequest:
                var audioRequestPacket = PacketPool<VcAudioRequestPacket>.GetPacket();
                audioRequestPacket.Deserialize(reader);
                HandleAudioRequestPacket(audioRequestPacket);
                break;
            case VcPacketType.SetMuteRequest:
                var setMuteRequestPacket = PacketPool<VcSetMuteRequestPacket>.GetPacket();
                setMuteRequestPacket.Deserialize(reader);
                HandleSetMuteRequestPacket(setMuteRequestPacket);
                break;
            case VcPacketType.SetDeafenRequest:
                var setDeafenRequestPacket = PacketPool<VcSetDeafenRequestPacket>.GetPacket();
                setDeafenRequestPacket.Deserialize(reader);
                HandleSetDeafenRequestPacket(setDeafenRequestPacket);
                break;
            case VcPacketType.SetTalkBitmaskRequest:
                var setTalkBitmaskRequestPacket = PacketPool<VcSetTalkBitmaskRequestPacket>.GetPacket();
                setTalkBitmaskRequestPacket.Deserialize(reader);
                HandleSetTalkBitmaskRequestPacket(setTalkBitmaskRequestPacket);
                break;
            case VcPacketType.SetListenBitmaskRequest:
                var setListenBitmaskRequestPacket = PacketPool<VcSetListenBitmaskRequestPacket>.GetPacket();
                setListenBitmaskRequestPacket.Deserialize(reader);
                HandleSetListenBitmaskRequestPacket(setListenBitmaskRequestPacket);
                break;
            case VcPacketType.SetEffectBitmaskRequest:
                var setEffectBitmaskRequestPacket = PacketPool<VcSetEffectBitmaskRequestPacket>.GetPacket();
                setEffectBitmaskRequestPacket.Deserialize(reader);
                HandleSetEffectBitmaskRequestPacket(setEffectBitmaskRequestPacket);
                break;
            case VcPacketType.SetPositionRequest:
                var setPositionRequestPacket = PacketPool<VcSetPositionRequestPacket>.GetPacket();
                setPositionRequestPacket.Deserialize(reader);
                HandleSetPositionBitmaskRequestPacket(setPositionRequestPacket);
                break;
            case VcPacketType.SetRotationRequest:
                var setRotationRequestPacket = PacketPool<VcSetRotationRequestPacket>.GetPacket();
                setRotationRequestPacket.Deserialize(reader);
                HandleSetRotationBitmaskRequestPacket(setRotationRequestPacket);
                break;
            case VcPacketType.SetCaveFactorRequest:
                var setCaveFactorRequestPacket = PacketPool<VcSetCaveFactorRequest>.GetPacket();
                setCaveFactorRequestPacket.Deserialize(reader);
                HandleSetCaveFactorRequestPacket(setCaveFactorRequestPacket);
                break;
            case VcPacketType.SetMuffleFactorRequest:
                var setMuffleFactorRequestPacket = PacketPool<VcSetMuffleFactorRequest>.GetPacket();
                setMuffleFactorRequestPacket.Deserialize(reader);
                HandleSetMuffleFactorRequestPacket(setMuffleFactorRequestPacket);
                break;
            case VcPacketType.SetTitleRequest:
                var setTitleRequestPacket = PacketPool<VcSetTitleRequestPacket>.GetPacket();
                setTitleRequestPacket.Deserialize(reader);
                HandleSetTitleRequestPacket(setTitleRequestPacket);
                break;
            case VcPacketType.SetDescriptionRequest:
                var setDescriptionRequestPacket = PacketPool<VcSetDescriptionRequestPacket>.GetPacket();
                setDescriptionRequestPacket.Deserialize(reader);
                HandleSetDescriptionRequestPacket(setDescriptionRequestPacket);
                break;
            case VcPacketType.SetEntityVisibilityRequest:
                var setEntityVisibilityRequestPacket = PacketPool<VcSetEntityVisibilityRequestPacket>.GetPacket();
                setEntityVisibilityRequestPacket.Deserialize(reader);
                HandleSetEntityVisibilityRequestPacket(setEntityVisibilityRequestPacket);
                break;

            //Responses
            case VcPacketType.InfoResponse:
                var infoResponsePacket = PacketPool<VcInfoResponsePacket>.GetPacket();
                infoResponsePacket.Deserialize(reader);
                HandleInfoResponsePacket(infoResponsePacket);
                break;
            case VcPacketType.AcceptResponse:
                var acceptResponsePacket = PacketPool<VcAcceptResponsePacket>.GetPacket();
                acceptResponsePacket.Deserialize(reader);
                HandleAcceptResponsePacket(acceptResponsePacket);
                break;
            case VcPacketType.DenyResponse:
                var denyResponsePacket = PacketPool<VcDenyResponsePacket>.GetPacket();
                denyResponsePacket.Deserialize(reader);
                HandleDenyResponsePacket(denyResponsePacket);
                break;

            //Events
            case VcPacketType.OnEffectUpdated:
                var onEffectUpdatedPacket = PacketPool<VcOnEffectUpdatedPacket>.GetPacket();
                onEffectUpdatedPacket.Deserialize(reader);
                HandleOnEffectUpdatedPacket(onEffectUpdatedPacket, reader);
                break;
            case VcPacketType.OnEntityCreated:
                var onEntityCreatedPacket = PacketPool<VcOnEntityCreatedPacket>.GetPacket();
                onEntityCreatedPacket.Deserialize(reader);
                HandleOnEntityCreatedPacket(onEntityCreatedPacket);
                break;
            case VcPacketType.OnNetworkEntityCreated:
                var onNetworkEntityCreatedPacket = PacketPool<VcOnNetworkEntityCreatedPacket>.GetPacket();
                onNetworkEntityCreatedPacket.Deserialize(reader);
                HandleOnNetworkEntityCreatedPacket(onNetworkEntityCreatedPacket);
                break;
            case VcPacketType.OnEntityDestroyed:
                var onEntityDestroyedPacket = PacketPool<VcOnEntityDestroyedPacket>.GetPacket();
                onEntityDestroyedPacket.Deserialize(reader);
                HandleOnEntityDestroyedPacket(onEntityDestroyedPacket);
                break;
            case VcPacketType.OnEntityNameUpdated:
                var onEntityNameUpdatedPacket = PacketPool<VcOnEntityNameUpdatedPacket>.GetPacket();
                onEntityNameUpdatedPacket.Deserialize(reader);
                HandleOnEntityNameUpdatedPacket(onEntityNameUpdatedPacket);
                break;
            case VcPacketType.OnEntityMuteUpdated:
                var onEntityMuteUpdatedPacket = PacketPool<VcOnEntityMuteUpdatedPacket>.GetPacket();
                onEntityMuteUpdatedPacket.Deserialize(reader);
                HandleOnEntityMuteUpdatedPacket(onEntityMuteUpdatedPacket);
                break;
            case VcPacketType.OnEntityDeafenUpdated:
                var onEntityDeafenUpdatedPacket = PacketPool<VcOnEntityDeafenUpdatedPacket>.GetPacket();
                onEntityDeafenUpdatedPacket.Deserialize(reader);
                HandleOnEntityDeafenUpdatedPacket(onEntityDeafenUpdatedPacket);
                break;
            case VcPacketType.OnEntityTalkBitmaskUpdated:
                var onEntityTalkBitmaskUpdatedPacket = PacketPool<VcOnEntityTalkBitmaskUpdatedPacket>.GetPacket();
                onEntityTalkBitmaskUpdatedPacket.Deserialize(reader);
                HandleOnEntityTalkBitmaskUpdatedPacket(onEntityTalkBitmaskUpdatedPacket);
                break;
            case VcPacketType.OnEntityListenBitmaskUpdated:
                var onEntityListenBitmaskUpdatedPacket = PacketPool<VcOnEntityListenBitmaskUpdatedPacket>.GetPacket();
                onEntityListenBitmaskUpdatedPacket.Deserialize(reader);
                HandleOnEntityListenBitmaskUpdatedPacket(onEntityListenBitmaskUpdatedPacket);
                break;
            case VcPacketType.OnEntityEffectBitmaskUpdated:
                var onEntityEffectBitmaskUpdatedPacket = PacketPool<VcOnEntityEffectBitmaskUpdatedPacket>.GetPacket();
                onEntityEffectBitmaskUpdatedPacket.Deserialize(reader);
                HandleOnEntityEffectBitmaskUpdatedPacket(onEntityEffectBitmaskUpdatedPacket);
                break;
            case VcPacketType.OnEntityPositionUpdated:
                var onEntityPositionUpdatedPacket = PacketPool<VcOnEntityPositionUpdatedPacket>.GetPacket();
                onEntityPositionUpdatedPacket.Deserialize(reader);
                HandleOnEntityPositionUpdatedPacket(onEntityPositionUpdatedPacket);
                break;
            case VcPacketType.OnEntityRotationUpdated:
                var onEntityRotationUpdatedPacket = PacketPool<VcOnEntityRotationUpdatedPacket>.GetPacket();
                onEntityRotationUpdatedPacket.Deserialize(reader);
                HandleOnEntityRotationUpdatedPacket(onEntityRotationUpdatedPacket);
                break;
            case VcPacketType.OnEntityCaveFactorUpdated:
                var onEntityCaveFactorUpdatedPacket = PacketPool<VcOnEntityCaveFactorUpdatedPacket>.GetPacket();
                onEntityCaveFactorUpdatedPacket.Deserialize(reader);
                HandleOnEntityCaveFactorUpdatedPacket(onEntityCaveFactorUpdatedPacket);
                break;
            case VcPacketType.OnEntityMuffleFactorUpdated:
                var onEntityMuffleFactorUpdatedPacket = PacketPool<VcOnEntityMuffleFactorUpdatedPacket>.GetPacket();
                onEntityMuffleFactorUpdatedPacket.Deserialize(reader);
                HandleOnEntityMuffleFactorUpdatedPacket(onEntityMuffleFactorUpdatedPacket);
                break;
            case VcPacketType.OnEntityAudioReceived:
                var onEntityAudioReceivedPacket = PacketPool<VcOnEntityAudioReceivedPacket>.GetPacket();
                onEntityAudioReceivedPacket.Deserialize(reader);
                HandleOnEntityAudioReceivedPacket(onEntityAudioReceivedPacket);
                break;
            default:
                return;
        }
    }

    private void ProcessUnconnectedPacket(VcPacketType packetType, NetPacketReader reader)
    {
        switch (packetType)
        {
            case VcPacketType.InfoResponse:
                var infoResponsePacket = PacketPool<VcInfoResponsePacket>.GetPacket();
                infoResponsePacket.Deserialize(reader);
                HandleInfoResponsePacket(infoResponsePacket);
                break;
            default:
                return;
        }
    }

    private void HandleInfoRequestPacket(VcInfoRequestPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
        }
        finally
        {
            PacketPool<VcInfoRequestPacket>.Return(packet);
        }
    }

    private void HandleLoginRequestPacket(VcLoginRequestPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
        }
        finally
        {
            PacketPool<VcLoginRequestPacket>.Return(packet);
        }
    }

    private void HandleLogoutRequestPacket(VcLogoutRequestPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
        }
        finally
        {
            PacketPool<VcLogoutRequestPacket>.Return(packet);
        }
    }

    private void HandleSetNameRequestPacket(VcSetNameRequestPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            Name = packet.Value;
        }
        finally
        {
            PacketPool<VcSetNameRequestPacket>.Return(packet);
        }
    }

    private void HandleAudioRequestPacket(VcAudioRequestPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
        }
        finally
        {
            PacketPool<VcAudioRequestPacket>.Return(packet);
        }
    }

    private void HandleSetMuteRequestPacket(VcSetMuteRequestPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
        }
        finally
        {
            PacketPool<VcSetMuteRequestPacket>.Return(packet);
        }
    }

    private void HandleSetDeafenRequestPacket(VcSetDeafenRequestPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
        }
        finally
        {
            PacketPool<VcSetDeafenRequestPacket>.Return(packet);
        }
    }

    private void HandleSetTalkBitmaskRequestPacket(VcSetTalkBitmaskRequestPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            TalkBitmask = packet.Value;
        }
        finally
        {
            PacketPool<VcSetTalkBitmaskRequestPacket>.Return(packet);
        }
    }

    private void HandleSetListenBitmaskRequestPacket(VcSetListenBitmaskRequestPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            ListenBitmask = packet.Value;
        }
        finally
        {
            PacketPool<VcSetListenBitmaskRequestPacket>.Return(packet);
        }
    }

    private void HandleSetEffectBitmaskRequestPacket(VcSetEffectBitmaskRequestPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            EffectBitmask = packet.Value;
        }
        finally
        {
            PacketPool<VcSetEffectBitmaskRequestPacket>.Return(packet);
        }
    }

    private void HandleSetPositionBitmaskRequestPacket(VcSetPositionRequestPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            Position = packet.Value;
        }
        finally
        {
            PacketPool<VcSetPositionRequestPacket>.Return(packet);
        }
    }

    private void HandleSetRotationBitmaskRequestPacket(VcSetRotationRequestPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            Rotation = packet.Value;
        }
        finally
        {
            PacketPool<VcSetRotationRequestPacket>.Return(packet);
        }
    }

    private void HandleSetCaveFactorRequestPacket(VcSetCaveFactorRequest packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            CaveFactor = packet.Value;
        }
        finally
        {
            PacketPool<VcSetCaveFactorRequest>.Return(packet);
        }
    }

    private void HandleSetMuffleFactorRequestPacket(VcSetMuffleFactorRequest packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            MuffleFactor = packet.Value;
        }
        finally
        {
            PacketPool<VcSetMuffleFactorRequest>.Return(packet);
        }
    }

    private void HandleSetTitleRequestPacket(VcSetTitleRequestPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            OnSetTitle?.Invoke(packet.Value);
        }
        finally
        {
            PacketPool<VcSetTitleRequestPacket>.Return(packet);
        }
    }

    private void HandleSetDescriptionRequestPacket(VcSetDescriptionRequestPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            OnSetDescription?.Invoke(packet.Value);
        }
        finally
        {
            PacketPool<VcSetDescriptionRequestPacket>.Return(packet);
        }
    }

    private void HandleSetEntityVisibilityRequestPacket(VcSetEntityVisibilityRequestPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            var entity = World.GetEntity(packet.Id);
            if (entity is not VoiceCraftClientEntity clientEntity) return;
            clientEntity.IsVisible = packet.Value;
            if (clientEntity.IsVisible) return; //Clear properties and the audio buffer when entity is not visible.
            clientEntity.ClearBuffer();
        }
        finally
        {
            PacketPool<VcSetEntityVisibilityRequestPacket>.Return(packet);
        }
    }

    private void HandleInfoResponsePacket(VcInfoResponsePacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            OnServerInfo?.Invoke(new ServerInfo(packet));
        }
        finally
        {
            PacketPool<VcInfoResponsePacket>.Return(packet);
        }
    }

    private void HandleAcceptResponsePacket(VcAcceptResponsePacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
        }
        finally
        {
            PacketPool<VcAcceptResponsePacket>.Return(packet);
        }
    }

    private void HandleDenyResponsePacket(VcDenyResponsePacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
        }
        finally
        {
            PacketPool<VcDenyResponsePacket>.Return(packet);
        }
    }

    private void HandleOnEffectUpdatedPacket(VcOnEffectUpdatedPacket packet, NetDataReader reader)
    {
        try
        {
            OnPacket?.Invoke(packet);
            if (_audioSystem.TryGetEffect(packet.Bitmask, out var effect) && effect.EffectType == packet.EffectType)
            {
                effect.Deserialize(reader); //Do not recreate the effect instance! Could hold audio instance data!
                return;
            }
            if(packet.EffectType == EffectType.None)
            {
                _audioSystem.SetEffect(packet.Bitmask, null);
                return;
            }
            var audioEffect = IAudioEffect.FromReader(packet.EffectType, reader);
            if(audioEffect == null) return;
            _audioSystem.SetEffect(packet.Bitmask, audioEffect);
        }
        finally
        {
            PacketPool<VcOnEffectUpdatedPacket>.Return(packet);
        }
    }

    private void HandleOnEntityCreatedPacket(VcOnEntityCreatedPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            var entity = new VoiceCraftClientEntity(packet.Id, World)
            {
                Name = packet.Name,
                Muted = packet.Muted,
                Deafened = packet.Deafened
            };
            World.AddEntity(entity);
        }
        finally
        {
            PacketPool<VcOnEntityCreatedPacket>.Return(packet);
        }
    }

    private void HandleOnNetworkEntityCreatedPacket(VcOnNetworkEntityCreatedPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            var entity = new VoiceCraftClientNetworkEntity(packet.Id, World, packet.UserGuid)
            {
                Name = packet.Name,
                Muted = packet.Muted,
                Deafened = packet.Deafened
            };
            World.AddEntity(entity);
        }
        finally
        {
            PacketPool<VcOnNetworkEntityCreatedPacket>.Return(packet);
        }
    }

    private void HandleOnEntityDestroyedPacket(VcOnEntityDestroyedPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            World.DestroyEntity(packet.Id);
        }
        finally
        {
            PacketPool<VcOnEntityDestroyedPacket>.Return(packet);
        }
    }

    private void HandleOnEntityNameUpdatedPacket(VcOnEntityNameUpdatedPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            var entity = World.GetEntity(packet.Id);
            if (entity == null) return;
            entity.Name = packet.Value;
        }
        finally
        {
            PacketPool<VcOnEntityNameUpdatedPacket>.Return(packet);
        }
    }

    private void HandleOnEntityMuteUpdatedPacket(VcOnEntityMuteUpdatedPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            var entity = World.GetEntity(packet.Id);
            if (entity == null) return;
            entity.Muted = packet.Value;
        }
        finally
        {
            PacketPool<VcOnEntityMuteUpdatedPacket>.Return(packet);
        }
    }

    private void HandleOnEntityDeafenUpdatedPacket(VcOnEntityDeafenUpdatedPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            var entity = World.GetEntity(packet.Id);
            if (entity == null) return;
            entity.Deafened = packet.Value;
        }
        finally
        {
            PacketPool<VcOnEntityDeafenUpdatedPacket>.Return(packet);
        }
    }

    private void HandleOnEntityTalkBitmaskUpdatedPacket(VcOnEntityTalkBitmaskUpdatedPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            var entity = World.GetEntity(packet.Id);
            if (entity == null) return;
            entity.TalkBitmask = packet.Value;
        }
        finally
        {
            PacketPool<VcOnEntityTalkBitmaskUpdatedPacket>.Return(packet);
        }
    }

    private void HandleOnEntityListenBitmaskUpdatedPacket(VcOnEntityListenBitmaskUpdatedPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            var entity = World.GetEntity(packet.Id);
            if (entity == null) return;
            entity.ListenBitmask = packet.Value;
        }
        finally
        {
            PacketPool<VcOnEntityListenBitmaskUpdatedPacket>.Return(packet);
        }
    }

    private void HandleOnEntityEffectBitmaskUpdatedPacket(VcOnEntityEffectBitmaskUpdatedPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            var entity = World.GetEntity(packet.Id);
            if (entity == null) return;
            entity.EffectBitmask = packet.Value;
        }
        finally
        {
            PacketPool<VcOnEntityEffectBitmaskUpdatedPacket>.Return(packet);
        }
    }

    private void HandleOnEntityPositionUpdatedPacket(VcOnEntityPositionUpdatedPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            var entity = World.GetEntity(packet.Id);
            if (entity == null) return;
            entity.Position = packet.Value;
        }
        finally
        {
            PacketPool<VcOnEntityPositionUpdatedPacket>.Return(packet);
        }
    }

    private void HandleOnEntityRotationUpdatedPacket(VcOnEntityRotationUpdatedPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            var entity = World.GetEntity(packet.Id);
            if (entity == null) return;
            entity.Rotation = packet.Value;
        }
        finally
        {
            PacketPool<VcOnEntityRotationUpdatedPacket>.Return(packet);
        }
    }

    private void HandleOnEntityCaveFactorUpdatedPacket(VcOnEntityCaveFactorUpdatedPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            var entity = World.GetEntity(packet.Id);
            if (entity == null) return;
            entity.CaveFactor = packet.Value;
        }
        finally
        {
            PacketPool<VcOnEntityCaveFactorUpdatedPacket>.Return(packet);
        }
    }

    private void HandleOnEntityMuffleFactorUpdatedPacket(VcOnEntityMuffleFactorUpdatedPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            var entity = World.GetEntity(packet.Id);
            if (entity == null) return;
            entity.MuffleFactor = packet.Value;
        }
        finally
        {
            PacketPool<VcOnEntityMuffleFactorUpdatedPacket>.Return(packet);
        }
    }

    private void HandleOnEntityAudioReceivedPacket(VcOnEntityAudioReceivedPacket packet)
    {
        try
        {
            OnPacket?.Invoke(packet);
            if (Deafened) return;
            var entity = World.GetEntity(packet.Id);
            entity?.ReceiveAudio(packet.Data, packet.Timestamp, packet.FrameLoudness);
        }
        finally
        {
            PacketPool<VcOnEntityAudioReceivedPacket>.Return(packet);
        }
    }
}