using System;
using System.Net;
using System.Runtime.InteropServices;
using LiteNetLib;
using LiteNetLib.Utils;
using OpusSharp.Core;
using OpusSharp.Core.Extensions;
using VoiceCraft.Client.Network.Systems;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio.Effects;
using VoiceCraft.Core.Network.Packets;
using VoiceCraft.Core.World;

namespace VoiceCraft.Client.Network;

public class VoiceCraftClient : VoiceCraftEntity, IDisposable
{
    public static readonly Version Version = new(1, 1, 0);

    //Systems
    private readonly AudioSystem _audioSystem;

    //Buffers
    private readonly NetDataWriter _dataWriter = new();
    private readonly byte[] _encodeBuffer = new byte[Constants.MaximumEncodedBytes];

    //Encoder
    private readonly OpusEncoder _encoder;
    private readonly EventBasedNetListener _listener;
    private readonly NetManager _netManager;

    //Networking
    private int _id = -1;

    private bool _isDisposed;
    private DateTime _lastAudioPeakTime = DateTime.MinValue;
    private uint _sendTimestamp;

    //Privates
    private NetPeer? _serverPeer;
    private bool _speakingState;

    public VoiceCraftClient() : base(0, new VoiceCraftWorld())
    {
        _listener = new EventBasedNetListener();
        _netManager = new NetManager(_listener)
        {
            AutoRecycle = true,
            IPv6Enabled = false,
            UnconnectedMessagesEnabled = true
        };

        _encoder = new OpusEncoder(Constants.SampleRate, Constants.Channels,
            OpusPredefinedValues.OPUS_APPLICATION_VOIP);
        _encoder.SetPacketLostPercent(50); //Expected packet loss, might make this change over time later.
        _encoder.SetBitRate(32000);

        //Setup Systems.
        _audioSystem = new AudioSystem(this, World);

        //Setup Listeners
        _listener.PeerConnectedEvent += OnConnectedEvent;
        _listener.PeerDisconnectedEvent += OnDisconnectedEvent;
        _listener.ConnectionRequestEvent += OnConnectionRequestEvent;
        _listener.NetworkReceiveEvent += OnNetworkReceiveEvent;
        _listener.NetworkReceiveUnconnectedEvent += OnNetworkReceiveUnconnectedEvent;

        //Start
        _netManager.Start();
    }

    //Public Properties
    public override int Id => _id;
    public ConnectionState ConnectionState => _serverPeer?.ConnectionState ?? ConnectionState.Disconnected;
    public float MicrophoneSensitivity { get; set; }

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

    ~VoiceCraftClient()
    {
        Dispose(false);
    }

    public bool Ping(string ip, uint port)
    {
        var packet = new InfoPacket(tick: Environment.TickCount);
        try
        {
            SendUnconnectedPacket(ip, port, packet);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Connect(Guid userGuid, Guid serverUserGuid, string ip, int port, string locale)
    {
        ThrowIfDisposed();
        if (ConnectionState != ConnectionState.Disconnected)
            throw new InvalidOperationException("This client is already connected or is connecting to a server!");

        _speakingState = false;
        lock (_dataWriter)
        {
            _dataWriter.Reset();
            var loginPacket = new LoginPacket(userGuid, serverUserGuid, locale, Version);
            loginPacket.Serialize(_dataWriter);
            _serverPeer = _netManager.Connect(ip, port, _dataWriter) ??
                          throw new InvalidOperationException("A connection request is awaiting!");
        }
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
        //Only enumerate over visible entities.
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
            ConnectionState != ConnectionState.Connected || Muted) return;

        Array.Clear(_encodeBuffer);
        var bytesEncoded = _encoder.Encode(buffer, Constants.SamplesPerFrame, _encodeBuffer, _encodeBuffer.Length);
        var packet = new AudioPacket(_serverPeer.RemoteId, _sendTimestamp, frameLoudness, bytesEncoded, _encodeBuffer);
        SendPacket(packet);
    }

    public void Disconnect()
    {
        if (_isDisposed || ConnectionState == ConnectionState.Disconnected) return;
        _netManager.DisconnectAll();
    }

    public bool SendPacket<T>(T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        where T : VoiceCraftPacket
    {
        if (ConnectionState != ConnectionState.Connected) return false;

        lock (_dataWriter)
        {
            _dataWriter.Reset();
            _dataWriter.Put((byte)packet.PacketType);
            packet.Serialize(_dataWriter);
            _serverPeer?.Send(_dataWriter, deliveryMethod);
            return true;
        }
    }

    public bool SendUnconnectedPacket<T>(IPEndPoint remoteEndPoint, T packet) where T : VoiceCraftPacket
    {
        lock (_dataWriter)
        {
            _dataWriter.Reset();
            _dataWriter.Put((byte)packet.PacketType);
            packet.Serialize(_dataWriter);
            return _netManager.SendUnconnectedMessage(_dataWriter, remoteEndPoint);
        }
    }

    public bool SendUnconnectedPacket<T>(string ip, uint port, T packet) where T : VoiceCraftPacket
    {
        lock (_dataWriter)
        {
            _dataWriter.Reset();
            _dataWriter.Put((byte)packet.PacketType);
            packet.Serialize(_dataWriter);
            return _netManager.SendUnconnectedMessage(_dataWriter, ip, (int)port);
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

            _listener.PeerConnectedEvent -= OnConnectedEvent;
            _listener.PeerDisconnectedEvent -= OnDisconnectedEvent;
            _listener.ConnectionRequestEvent -= OnConnectionRequestEvent;
            _listener.NetworkReceiveEvent -= OnNetworkReceiveEvent;
            _listener.NetworkReceiveUnconnectedEvent -= OnNetworkReceiveUnconnectedEvent;

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

    //Network Handling
    private void OnConnectedEvent(NetPeer peer)
    {
        if (!Equals(peer, _serverPeer)) return;
        OnConnected?.Invoke();
    }

    private void OnDisconnectedEvent(NetPeer peer, DisconnectInfo info)
    {
        if (!Equals(peer, _serverPeer)) return;
        try
        {
            World.ClearEntities();

            if (info.AdditionalData.IsNull)
            {
                OnDisconnected?.Invoke(info.Reason.ToString());
                return;
            }

            var logoutPacket = new LogoutPacket();
            logoutPacket.Deserialize(info.AdditionalData);
            OnDisconnected?.Invoke(logoutPacket.Reason);
        }
        catch
        {
            OnDisconnected?.Invoke(info.Reason.ToString());
        }
        finally
        {
            _id = -1;
        }
    }

    private static void OnConnectionRequestEvent(ConnectionRequest request)
    {
        request.Reject(); //No fuck you.
    }

    private void OnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel,
        DeliveryMethod deliveryMethod)
    {
        try
        {
            var packetType = reader.GetByte();
            var pt = (PacketType)packetType;
            ProcessPacket(pt, reader);
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
        }

        reader.Recycle();
    }

    private void OnNetworkReceiveUnconnectedEvent(IPEndPoint remoteEndPoint, NetPacketReader reader,
        UnconnectedMessageType messageType)
    {
        try
        {
            var packetType = reader.GetByte();
            var pt = (PacketType)packetType;
            ProcessPacket(pt, reader);
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
        }

        reader.Recycle();
    }

    //Packet Handling
    private void ProcessPacket(PacketType packetType, NetPacketReader reader)
    {
        switch (packetType)
        {
            case PacketType.Info:
                var infoPacket = new InfoPacket();
                infoPacket.Deserialize(reader);
                HandleInfoPacket(infoPacket);
                break;
            case PacketType.SetId:
                var setIdPacket = new SetIdPacket();
                setIdPacket.Deserialize(reader);
                HandleSetIdPacket(setIdPacket);
                break;
            case PacketType.SetEffect:
                var setEffectPacket = new SetEffectPacket();
                setEffectPacket.Deserialize(reader);
                HandleSetEffectPacket(setEffectPacket, reader);
                break;
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
            case PacketType.SetDescription:
                var setDescriptionPacket = new SetDescriptionPacket();
                setDescriptionPacket.Deserialize(reader);
                HandleSetDescriptionPacket(setDescriptionPacket);
                break;
            case PacketType.EntityCreated:
                var entityCreatedPacket = new EntityCreatedPacket();
                entityCreatedPacket.Deserialize(reader);
                HandleEntityCreatedPacket(entityCreatedPacket);
                break;
            case PacketType.NetworkEntityCreated:
                var networkEntityCreatedPacket = new NetworkEntityCreatedPacket();
                networkEntityCreatedPacket.Deserialize(reader);
                HandleNetworkEntityCreatedPacket(networkEntityCreatedPacket);
                break;
            case PacketType.EntityDestroyed:
                var entityDestroyedPacket = new EntityDestroyedPacket();
                entityDestroyedPacket.Deserialize(reader);
                HandleEntityDestroyedPacket(entityDestroyedPacket);
                break;
            case PacketType.SetVisibility:
                var setVisibilityPacket = new SetVisibilityPacket();
                setVisibilityPacket.Deserialize(reader);
                HandleSetVisibilityPacket(setVisibilityPacket);
                break;
            case PacketType.SetName:
                var setNamePacket = new SetNamePacket();
                setNamePacket.Deserialize(reader);
                HandleSetNamePacket(setNamePacket);
                break;
            case PacketType.SetMute:
                var setMutePacket = new SetMutePacket();
                setMutePacket.Deserialize(reader);
                HandleSetMutePacket(setMutePacket);
                break;
            case PacketType.SetDeafen:
                var setDeafen = new SetDeafenPacket();
                setDeafen.Deserialize(reader);
                HandleSetDeafenPacket(setDeafen);
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
            case PacketType.SetEffectBitmask:
                var setEffectBitmaskPacket = new SetEffectBitmaskPacket();
                setEffectBitmaskPacket.Deserialize(reader);
                HandleSetEffectBitmaskPacket(setEffectBitmaskPacket);
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
            case PacketType.SetCaveFactor:
                var setCaveFactorPacket = new SetCaveFactorPacket();
                setCaveFactorPacket.Deserialize(reader);
                HandleSetCaveFactorPacket(setCaveFactorPacket);
                break;
            case PacketType.SetMuffleFactor:
                var setMuffleFactorPacket = new SetMuffleFactorPacket();
                setMuffleFactorPacket.Deserialize(reader);
                HandleSetMuffleFactorPacket(setMuffleFactorPacket);
                break;
            case PacketType.Login:
            case PacketType.Logout:
            default:
                break;
        }
    }

    private void HandleInfoPacket(InfoPacket infoPacket)
    {
        OnServerInfo?.Invoke(new ServerInfo(infoPacket));
    }

    private void HandleSetIdPacket(SetIdPacket setIdPacket)
    {
        _id = setIdPacket.Id;
    }

    private void HandleSetEffectPacket(SetEffectPacket packet, NetDataReader reader)
    {
        if (_audioSystem.TryGetEffect(packet.Bitmask, out var effect) && effect.EffectType == packet.EffectType)
        {
            effect.Deserialize(reader); //Do not recreate the effect instance! Could hold audio instance data!
            return;
        }

        switch (packet.EffectType)
        {
            case EffectType.Visibility:
                var visibilityEffect = new VisibilityEffect();
                visibilityEffect.Deserialize(reader);
                _audioSystem.SetEffect(packet.Bitmask, visibilityEffect);
                break;
            case EffectType.Proximity:
                var proximityEffect = new ProximityEffect();
                proximityEffect.Deserialize(reader);
                _audioSystem.SetEffect(packet.Bitmask, proximityEffect);
                break;
            case EffectType.Directional:
                var directionalEffect = new DirectionalEffect();
                directionalEffect.Deserialize(reader);
                _audioSystem.SetEffect(packet.Bitmask, directionalEffect);
                break;
            case EffectType.ProximityEcho:
                var proximityEchoEffect = new ProximityEchoEffect(Constants.SampleRate, 0.5f);
                proximityEchoEffect.Deserialize(reader);
                _audioSystem.SetEffect(packet.Bitmask, proximityEchoEffect);
                break;
            case EffectType.Echo:
                var echoEffect = new EchoEffect(Constants.SampleRate, 0.5f);
                echoEffect.Deserialize(reader);
                _audioSystem.SetEffect(packet.Bitmask, echoEffect);
                break;
            case EffectType.None:
                _audioSystem.SetEffect(packet.Bitmask, null);
                break;
            default: //Unknown, We don't do anything.
                return;
        }
    }

    private void HandleAudioPacket(AudioPacket packet)
    {
        if (Deafened) return;
        var entity = World.GetEntity(packet.Id);
        entity?.ReceiveAudio(packet.Data, packet.Timestamp, packet.FrameLoudness);
    }

    private void HandleSetTitlePacket(SetTitlePacket packet)
    {
        OnSetTitle?.Invoke(packet.Value);
    }

    private void HandleSetDescriptionPacket(SetDescriptionPacket packet)
    {
        OnSetDescription?.Invoke(packet.Value);
    }

    private void HandleEntityCreatedPacket(EntityCreatedPacket packet)
    {
        var entity = new VoiceCraftClientEntity(packet.Id, World)
        {
            Name = packet.Name,
            Muted = packet.Muted,
            Deafened = packet.Deafened
        };
        World.AddEntity(entity);
    }

    private void HandleNetworkEntityCreatedPacket(NetworkEntityCreatedPacket packet)
    {
        if (packet.Id == Id)
        {
            Name = packet.Name;
            //We don't want to update anything else as it may be a concern for privacy.
            return;
        }

        var entity = new VoiceCraftClientNetworkEntity(packet.Id, World, packet.UserGuid)
        {
            Name = packet.Name,
            Muted = packet.Muted,
            Deafened = packet.Deafened
        };
        World.AddEntity(entity);
    }

    private void HandleEntityDestroyedPacket(EntityDestroyedPacket packet)
    {
        World.DestroyEntity(packet.Id);
    }

    private void HandleSetVisibilityPacket(SetVisibilityPacket packet)
    {
        var entity = World.GetEntity(packet.Id);
        if (entity is not VoiceCraftClientEntity clientEntity) return;
        clientEntity.IsVisible = packet.Value;
        if (clientEntity.IsVisible) return; //Clear properties and the audio buffer when entity is not visible.
        clientEntity.ClearBuffer();
    }

    private void HandleSetNamePacket(SetNamePacket packet)
    {
        if (packet.Id == Id)
        {
            Name = packet.Value;
            return;
        }

        var entity = World.GetEntity(packet.Id);
        if (entity == null) return;
        entity.Name = packet.Value;
    }

    private void HandleSetMutePacket(SetMutePacket packet)
    {
        var entity = World.GetEntity(packet.Id);
        if (entity == null) return;
        entity.Muted = packet.Value;
    }

    private void HandleSetDeafenPacket(SetDeafenPacket packet)
    {
        var entity = World.GetEntity(packet.Id);
        if (entity == null) return;
        entity.Deafened = packet.Value;
    }

    private void HandleSetTalkBitmaskPacket(SetTalkBitmaskPacket packet)
    {
        if (packet.Id == Id)
        {
            TalkBitmask = packet.Value;
            return;
        }

        var entity = World.GetEntity(packet.Id);
        if (entity == null) return;
        entity.TalkBitmask = packet.Value;
    }

    private void HandleSetListenBitmaskPacket(SetListenBitmaskPacket packet)
    {
        if (packet.Id == Id)
        {
            ListenBitmask = packet.Value;
            return;
        }

        var entity = World.GetEntity(packet.Id);
        if (entity == null) return;
        entity.ListenBitmask = packet.Value;
    }

    private void HandleSetEffectBitmaskPacket(SetEffectBitmaskPacket packet)
    {
        if (packet.Id == Id)
        {
            EffectBitmask = packet.Value;
            return;
        }

        var entity = World.GetEntity(packet.Id);
        if (entity == null) return;
        entity.EffectBitmask = packet.Value;
    }

    private void HandleSetPositionPacket(SetPositionPacket packet)
    {
        if (packet.Id == Id)
        {
            Position = packet.Value;
            return;
        }

        var entity = World.GetEntity(packet.Id);
        if (entity == null) return;
        entity.Position = packet.Value;
    }

    private void HandleSetRotationPacket(SetRotationPacket packet)
    {
        if (packet.Id == Id)
        {
            Rotation = packet.Value;
            return;
        }

        var entity = World.GetEntity(packet.Id);
        if (entity == null) return;
        entity.Rotation = packet.Value;
    }

    private void HandleSetCaveFactorPacket(SetCaveFactorPacket packet)
    {
        if (packet.Id == Id)
        {
            CaveFactor = packet.Value;
            return;
        }

        var entity = World.GetEntity(packet.Id);
        if (entity == null) return;
        entity.CaveFactor = packet.Value;
    }

    private void HandleSetMuffleFactorPacket(SetMuffleFactorPacket packet)
    {
        if (packet.Id == Id)
        {
            MuffleFactor = packet.Value;
            return;
        }

        var entity = World.GetEntity(packet.Id);
        if (entity == null) return;
        entity.MuffleFactor = packet.Value;
    }
}