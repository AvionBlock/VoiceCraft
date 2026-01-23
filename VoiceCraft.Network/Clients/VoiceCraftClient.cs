using System;
using System.Buffers;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Packets.VcPackets;
using VoiceCraft.Network.Packets.VcPackets.Event;
using VoiceCraft.Network.Packets.VcPackets.Request;
using VoiceCraft.Network.Packets.VcPackets.Response;
using VoiceCraft.Network.Systems;
using VoiceCraft.Network.World;

namespace VoiceCraft.Network.Clients;

public abstract class VoiceCraftClient : VoiceCraftEntity, IDisposable
{
    protected bool Disposed;

    //Audio
    private readonly Func<IAudioDecoder> _audioDecoderFactory;
    private readonly IAudioEncoder _audioEncoder;
    private DateTime _lastAudioPeakTime = DateTime.MinValue;
    private float _microphoneSensitivity;
    private float _outputVolume;
    private ushort _sendTimestamp;
    private bool _serverDeafened;
    private bool _serverMuted;
    private bool _speakingState;

    public static Version Version { get; } = new(Constants.Major, Constants.Minor, Constants.Patch);
    public VoiceCraftWorld World { get; } = new();
    public AudioEffectSystem AudioEffectSystem { get; } = new();
    public abstract PositioningType PositioningType { get; }
    public VcConnectionState ConnectionState { get; protected set; }

    public float MicrophoneSensitivity
    {
        get => _microphoneSensitivity;
        set => _microphoneSensitivity = Math.Clamp(value, 0, 1);
    }

    public float OutputVolume
    {
        get => _outputVolume;
        set => _outputVolume = Math.Clamp(value, 0, 2);
    }

    public bool SpeakingState
    {
        get => _speakingState;
        private set
        {
            if (_speakingState == value) return;
            _speakingState = value;
            OnSpeakingUpdated?.Invoke(value);
        }
    }

    public bool ServerMuted
    {
        get => _serverMuted;
        private set
        {
            if (_serverMuted == value) return;
            _serverMuted = value;
            OnServerMuteUpdated?.Invoke(_serverMuted);
        }
    }

    public bool ServerDeafened
    {
        get => _serverDeafened;
        private set
        {
            if (_serverDeafened == value) return;
            _serverDeafened = value;
            OnServerDeafenUpdated?.Invoke(_serverDeafened);
        }
    }

    public event Action<IVoiceCraftPacket>? OnUnconnectedPacketReceived;
    public event Action<IVoiceCraftPacket>? OnPacketReceived;
    public abstract event Action? OnConnected;
    public abstract event Action<string?>? OnDisconnected;
    public event Action<string>? OnSetTitle;
    public event Action<string>? OnSetDescription;
    public event Action<bool>? OnSpeakingUpdated;
    public event Action<bool>? OnServerMuteUpdated;
    public event Action<bool>? OnServerDeafenUpdated;

    protected VoiceCraftClient(IAudioEncoder audioEncoder, Func<IAudioDecoder> decoderFactory) : base(0)
    {
        _audioEncoder = audioEncoder;
        _audioDecoderFactory = decoderFactory;

        //Internal Listeners
        OnWorldIdUpdated += OnClientWorldIdUpdated;
        OnNameUpdated += OnClientNameUpdated;
        OnMuteUpdated += OnClientMuteUpdated;
        OnDeafenUpdated += OnClientDeafenUpdated;
        OnPositionUpdated += OnClientPositionUpdated;
        OnRotationUpdated += OnClientRotationUpdated;
        OnCaveFactorUpdated += OnClientCaveFactorUpdated;
        OnMuffleFactorUpdated += OnClientMuffleFactorUpdated;
    }

    ~VoiceCraftClient()
    {
        Dispose(false);
    }

    public abstract Task<ServerInfo> PingAsync(string ip, int port, CancellationToken token = default);

    public abstract Task ConnectAsync(string ip, int port, Guid userGuid, Guid serverUserGuid, string locale,
        PositioningType positioningType);

    public abstract void Update();
    public abstract Task DisconnectAsync(string? reason = null);

    public abstract void SendUnconnectedPacket<T>(string ip, int port, T packet) where T : IVoiceCraftPacket;

    public abstract void SendPacket<T>(T packet,
        VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable) where T : IVoiceCraftPacket;

    public int Read(Span<float> buffer)
    {
        return AudioEffectSystem.Read(buffer, this);
    }

    public void Write(Span<float> buffer)
    {
        var frameLoudness = SampleLoudness.Read(buffer);
        if (frameLoudness >= MicrophoneSensitivity)
            _lastAudioPeakTime = DateTime.UtcNow;

        _sendTimestamp += 1; //Add to timestamp even though we aren't really connected.
        if ((DateTime.UtcNow - _lastAudioPeakTime).TotalMilliseconds > Constants.SilenceThresholdMs ||
            ConnectionState != VcConnectionState.Connected || Muted || ServerMuted)
        {
            SpeakingState = false;
            return;
        }

        SpeakingState = true;
        var encodeBuffer = ArrayPool<byte>.Shared.Rent(Constants.MaximumEncodedBytes);
        encodeBuffer.AsSpan().Clear();
        try
        {
            var bytesEncoded = _audioEncoder.Encode(buffer, encodeBuffer, Constants.SamplesPerFrame);
            SendPacket(PacketPool<VcAudioRequestPacket>.GetPacket()
                .Set(_sendTimestamp, frameLoudness, bytesEncoded, encodeBuffer));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(encodeBuffer);
        }
    }

    public override void Reset()
    {
        //Doesn't remove the entity from the world.
        Name = "New Client";
        CaveFactor = 0;
        MuffleFactor = 0;
        WorldId = string.Empty;
        Position = Vector3.Zero;
        Rotation = Vector2.Zero;
        EffectBitmask = ushort.MaxValue;
        TalkBitmask = ushort.MaxValue;
        ListenBitmask = ushort.MaxValue;
        ServerMuted = false;
        ServerDeafened = false;
        World.Reset();
        AudioEffectSystem.Reset();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void ProcessPacket(NetDataReader reader, Action<IVoiceCraftPacket> onParsed)
    {
        var packetType = (VcPacketType)reader.GetByte();
        switch (packetType)
        {
            case VcPacketType.LogoutRequest:
                ProcessPacket<VcLogoutRequestPacket>(reader, onParsed);
                break;
            case VcPacketType.InfoResponse:
                ProcessPacket<VcInfoResponsePacket>(reader, onParsed);
                break;
            case VcPacketType.AcceptResponse:
                ProcessPacket<VcAcceptResponsePacket>(reader, onParsed);
                break;
            case VcPacketType.DenyResponse:
                ProcessPacket<VcDenyResponsePacket>(reader, onParsed);
                break;
            case VcPacketType.SetNameRequest:
                ProcessPacket<VcSetNameRequestPacket>(reader, onParsed);
                break;
            case VcPacketType.SetServerMuteRequest:
                ProcessPacket<VcSetServerMuteRequestPacket>(reader, onParsed);
                break;
            case VcPacketType.SetServerDeafenRequest:
                ProcessPacket<VcSetServerDeafenRequestPacket>(reader, onParsed);
                break;
            case VcPacketType.SetWorldIdRequest:
                ProcessPacket<VcSetWorldIdRequestPacket>(reader, onParsed);
                break;
            case VcPacketType.SetTalkBitmaskRequest:
                ProcessPacket<VcSetTalkBitmaskRequestPacket>(reader, onParsed);
                break;
            case VcPacketType.SetListenBitmaskRequest:
                ProcessPacket<VcSetListenBitmaskRequestPacket>(reader, onParsed);
                break;
            case VcPacketType.SetEffectBitmaskRequest:
                ProcessPacket<VcSetEffectBitmaskRequestPacket>(reader, onParsed);
                break;
            case VcPacketType.SetPositionRequest:
                ProcessPacket<VcSetPositionRequestPacket>(reader, onParsed);
                break;
            case VcPacketType.SetRotationRequest:
                ProcessPacket<VcSetRotationRequestPacket>(reader, onParsed);
                break;
            case VcPacketType.SetCaveFactorRequest:
                ProcessPacket<VcSetCaveFactorRequest>(reader, onParsed);
                break;
            case VcPacketType.SetMuffleFactorRequest:
                ProcessPacket<VcSetMuffleFactorRequest>(reader, onParsed);
                break;
            case VcPacketType.SetTitleRequest:
                ProcessPacket<VcSetTitleRequestPacket>(reader, onParsed);
                break;
            case VcPacketType.SetDescriptionRequest:
                ProcessPacket<VcSetDescriptionRequestPacket>(reader, onParsed);
                break;
            case VcPacketType.SetEntityVisibilityRequest:
                ProcessPacket<VcSetEntityVisibilityRequestPacket>(reader, onParsed);
                break;
            case VcPacketType.OnEffectUpdated:
                ProcessPacket<VcOnEffectUpdatedPacket>(reader, onParsed);
                break;
            case VcPacketType.OnEntityCreated:
                ProcessPacket<VcOnEntityCreatedPacket>(reader, onParsed);
                break;
            case VcPacketType.OnNetworkEntityCreated:
                ProcessPacket<VcOnNetworkEntityCreatedPacket>(reader, onParsed);
                break;
            case VcPacketType.OnEntityDestroyed:
                ProcessPacket<VcOnEntityDestroyedPacket>(reader, onParsed);
                break;
            case VcPacketType.OnEntityNameUpdated:
                ProcessPacket<VcOnEntityNameUpdatedPacket>(reader, onParsed);
                break;
            case VcPacketType.OnEntityMuteUpdated:
                ProcessPacket<VcOnEntityMuteUpdatedPacket>(reader, onParsed);
                break;
            case VcPacketType.OnEntityDeafenUpdated:
                ProcessPacket<VcOnEntityDeafenUpdatedPacket>(reader, onParsed);
                break;
            case VcPacketType.OnEntityServerMuteUpdated:
                ProcessPacket<VcOnEntityServerMuteUpdatedPacket>(reader, onParsed);
                break;
            case VcPacketType.OnEntityServerDeafenUpdated:
                ProcessPacket<VcOnEntityServerDeafenUpdatedPacket>(reader, onParsed);
                break;
            case VcPacketType.OnEntityTalkBitmaskUpdated:
                ProcessPacket<VcOnEntityTalkBitmaskUpdatedPacket>(reader, onParsed);
                break;
            case VcPacketType.OnEntityListenBitmaskUpdated:
                ProcessPacket<VcOnEntityListenBitmaskUpdatedPacket>(reader, onParsed);
                break;
            case VcPacketType.OnEntityEffectBitmaskUpdated:
                ProcessPacket<VcOnEntityEffectBitmaskUpdatedPacket>(reader, onParsed);
                break;
            case VcPacketType.OnEntityPositionUpdated:
                ProcessPacket<VcOnEntityPositionUpdatedPacket>(reader, onParsed);
                break;
            case VcPacketType.OnEntityRotationUpdated:
                ProcessPacket<VcOnEntityRotationUpdatedPacket>(reader, onParsed);
                break;
            case VcPacketType.OnEntityCaveFactorUpdated:
                ProcessPacket<VcOnEntityCaveFactorUpdatedPacket>(reader, onParsed);
                break;
            case VcPacketType.OnEntityMuffleFactorUpdated:
                ProcessPacket<VcOnEntityMuffleFactorUpdatedPacket>(reader, onParsed);
                break;
            case VcPacketType.OnEntityAudioReceived:
                ProcessPacket<VcOnEntityAudioReceivedPacket>(reader, onParsed);
                break;
            case VcPacketType.InfoRequest:
            case VcPacketType.LoginRequest:
            case VcPacketType.AudioRequest:
            case VcPacketType.SetMuteRequest:
            case VcPacketType.SetDeafenRequest:
            default:
                return;
        }
    }

    protected void ProcessUnconnectedPacket(NetDataReader reader, Action<IVoiceCraftPacket> onParsed)
    {
        var packetType = (VcPacketType)reader.GetByte();
        switch (packetType)
        {
            case VcPacketType.InfoRequest:
            case VcPacketType.LoginRequest:
            case VcPacketType.LogoutRequest:
            case VcPacketType.InfoResponse:
                ProcessUnconnectedPacket<VcInfoResponsePacket>(reader, onParsed);
                break;
            case VcPacketType.AcceptResponse:
            case VcPacketType.DenyResponse:
            case VcPacketType.SetNameRequest:
            case VcPacketType.AudioRequest:
            case VcPacketType.SetMuteRequest:
            case VcPacketType.SetDeafenRequest:
            case VcPacketType.SetServerMuteRequest:
            case VcPacketType.SetServerDeafenRequest:
            case VcPacketType.SetWorldIdRequest:
            case VcPacketType.SetTalkBitmaskRequest:
            case VcPacketType.SetListenBitmaskRequest:
            case VcPacketType.SetEffectBitmaskRequest:
            case VcPacketType.SetPositionRequest:
            case VcPacketType.SetRotationRequest:
            case VcPacketType.SetCaveFactorRequest:
            case VcPacketType.SetMuffleFactorRequest:
            case VcPacketType.SetTitleRequest:
            case VcPacketType.SetDescriptionRequest:
            case VcPacketType.SetEntityVisibilityRequest:
            case VcPacketType.OnEffectUpdated:
            case VcPacketType.OnEntityCreated:
            case VcPacketType.OnNetworkEntityCreated:
            case VcPacketType.OnEntityDestroyed:
            case VcPacketType.OnEntityNameUpdated:
            case VcPacketType.OnEntityMuteUpdated:
            case VcPacketType.OnEntityDeafenUpdated:
            case VcPacketType.OnEntityServerMuteUpdated:
            case VcPacketType.OnEntityServerDeafenUpdated:
            case VcPacketType.OnEntityTalkBitmaskUpdated:
            case VcPacketType.OnEntityListenBitmaskUpdated:
            case VcPacketType.OnEntityEffectBitmaskUpdated:
            case VcPacketType.OnEntityPositionUpdated:
            case VcPacketType.OnEntityRotationUpdated:
            case VcPacketType.OnEntityCaveFactorUpdated:
            case VcPacketType.OnEntityMuffleFactorUpdated:
            case VcPacketType.OnEntityAudioReceived:
            default:
                return;
        }
    }

    protected void ExecutePacket(IVoiceCraftPacket packet)
    {
        switch (packet)
        {
            //Core. DO NOT CHANGE
            //Responses
            //Requests
            case VcSetNameRequestPacket setNameRequestPacket:
                HandleSetNameRequestPacket(setNameRequestPacket);
                break;
            case VcSetServerMuteRequestPacket setServerMuteRequestPacket:
                HandleSetServerMuteRequestPacket(setServerMuteRequestPacket);
                break;
            case VcSetServerDeafenRequestPacket setServerDeafenRequestPacket:
                HandleSetServerDeafenRequestPacket(setServerDeafenRequestPacket);
                break;
            case VcSetTalkBitmaskRequestPacket setTalkBitmaskRequestPacket:
                HandleSetTalkBitmaskRequestPacket(setTalkBitmaskRequestPacket);
                break;
            case VcSetListenBitmaskRequestPacket setListenBitmaskRequestPacket:
                HandleSetListenBitmaskRequestPacket(setListenBitmaskRequestPacket);
                break;
            case VcSetEffectBitmaskRequestPacket setEffectBitmaskRequestPacket:
                HandleSetEffectBitmaskRequestPacket(setEffectBitmaskRequestPacket);
                break;
            case VcSetPositionRequestPacket setPositionRequestPacket:
                HandleSetPositionBitmaskRequestPacket(setPositionRequestPacket);
                break;
            case VcSetRotationRequestPacket setRotationRequestPacket:
                HandleSetRotationBitmaskRequestPacket(setRotationRequestPacket);
                break;
            case VcSetCaveFactorRequest setCaveFactorRequestPacket:
                HandleSetCaveFactorRequestPacket(setCaveFactorRequestPacket);
                break;
            case VcSetMuffleFactorRequest setMuffleFactorRequestPacket:
                HandleSetMuffleFactorRequestPacket(setMuffleFactorRequestPacket);
                break;
            case VcSetTitleRequestPacket setTitleRequestPacket:
                HandleSetTitleRequestPacket(setTitleRequestPacket);
                break;
            case VcSetDescriptionRequestPacket setDescriptionRequestPacket:
                HandleSetDescriptionRequestPacket(setDescriptionRequestPacket);
                break;
            case VcSetEntityVisibilityRequestPacket setEntityVisibilityRequestPacket:
                HandleSetEntityVisibilityRequestPacket(setEntityVisibilityRequestPacket);
                break;

            //Responses

            //Events
            case VcOnEffectUpdatedPacket onEffectUpdatedPacket:
                HandleOnEffectUpdatedPacket(onEffectUpdatedPacket);
                break;
            case VcOnEntityCreatedPacket onEntityCreatedPacket:
                if (onEntityCreatedPacket is VcOnNetworkEntityCreatedPacket onNetworkEntityCreatedPacket)
                {
                    HandleOnNetworkEntityCreatedPacket(onNetworkEntityCreatedPacket);
                    break;
                }

                HandleOnEntityCreatedPacket(onEntityCreatedPacket);
                break;
            case VcOnEntityDestroyedPacket onEntityDestroyedPacket:
                HandleOnEntityDestroyedPacket(onEntityDestroyedPacket);
                break;
            case VcOnEntityNameUpdatedPacket onEntityNameUpdatedPacket:
                HandleOnEntityNameUpdatedPacket(onEntityNameUpdatedPacket);
                break;
            case VcOnEntityMuteUpdatedPacket onEntityMuteUpdatedPacket:
                HandleOnEntityMuteUpdatedPacket(onEntityMuteUpdatedPacket);
                break;
            case VcOnEntityDeafenUpdatedPacket onEntityDeafenUpdatedPacket:
                HandleOnEntityDeafenUpdatedPacket(onEntityDeafenUpdatedPacket);
                break;
            case VcOnEntityServerMuteUpdatedPacket onEntityServerMuteUpdatedPacket:
                HandleOnEntityServerMuteUpdatedPacket(onEntityServerMuteUpdatedPacket);
                break;
            case VcOnEntityServerDeafenUpdatedPacket onEntityServerDeafenUpdatedPacket:
                HandleOnEntityServerDeafenUpdatedPacket(onEntityServerDeafenUpdatedPacket);
                break;
            case VcOnEntityTalkBitmaskUpdatedPacket onEntityTalkBitmaskUpdatedPacket:
                HandleOnEntityTalkBitmaskUpdatedPacket(onEntityTalkBitmaskUpdatedPacket);
                break;
            case VcOnEntityListenBitmaskUpdatedPacket onEntityListenBitmaskUpdatedPacket:
                HandleOnEntityListenBitmaskUpdatedPacket(onEntityListenBitmaskUpdatedPacket);
                break;
            case VcOnEntityEffectBitmaskUpdatedPacket onEntityEffectBitmaskUpdatedPacket:
                HandleOnEntityEffectBitmaskUpdatedPacket(onEntityEffectBitmaskUpdatedPacket);
                break;
            case VcOnEntityPositionUpdatedPacket onEntityPositionUpdatedPacket:
                HandleOnEntityPositionUpdatedPacket(onEntityPositionUpdatedPacket);
                break;
            case VcOnEntityRotationUpdatedPacket onEntityRotationUpdatedPacket:
                HandleOnEntityRotationUpdatedPacket(onEntityRotationUpdatedPacket);
                break;
            case VcOnEntityCaveFactorUpdatedPacket onEntityCaveFactorUpdatedPacket:
                HandleOnEntityCaveFactorUpdatedPacket(onEntityCaveFactorUpdatedPacket);
                break;
            case VcOnEntityMuffleFactorUpdatedPacket onEntityMuffleFactorUpdatedPacket:
                HandleOnEntityMuffleFactorUpdatedPacket(onEntityMuffleFactorUpdatedPacket);
                break;
            case VcOnEntityAudioReceivedPacket onEntityAudioReceivedPacket:
                HandleOnEntityAudioReceivedPacket(onEntityAudioReceivedPacket);
                break;
            default:
                return;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (Disposed) return;
        if (disposing)
        {
            _audioEncoder.Dispose();
            World.Dispose();
            AudioEffectSystem.Dispose();

            Destroy();

            OnWorldIdUpdated -= OnClientWorldIdUpdated;
            OnNameUpdated -= OnClientNameUpdated;
            OnMuteUpdated -= OnClientMuteUpdated;
            OnDeafenUpdated -= OnClientDeafenUpdated;
            OnPositionUpdated -= OnClientPositionUpdated;
            OnRotationUpdated -= OnClientRotationUpdated;
            OnCaveFactorUpdated -= OnClientCaveFactorUpdated;
            OnMuffleFactorUpdated -= OnClientMuffleFactorUpdated;

            OnSetTitle = null;
            OnSetDescription = null;
            OnSpeakingUpdated = null;
            OnServerMuteUpdated = null;
            OnServerDeafenUpdated = null;
        }

        Disposed = true;
    }

    //Packet Handling
    private void HandleSetNameRequestPacket(VcSetNameRequestPacket packet)
    {
        Name = packet.Value;
    }

    private void HandleSetServerMuteRequestPacket(VcSetServerMuteRequestPacket packet)
    {
        ServerMuted = packet.Value;
    }

    private void HandleSetServerDeafenRequestPacket(VcSetServerDeafenRequestPacket packet)
    {
        ServerDeafened = packet.Value;
    }

    private void HandleSetTalkBitmaskRequestPacket(VcSetTalkBitmaskRequestPacket packet)
    {
        TalkBitmask = packet.Value;
    }

    private void HandleSetListenBitmaskRequestPacket(VcSetListenBitmaskRequestPacket packet)
    {
        ListenBitmask = packet.Value;
    }

    private void HandleSetEffectBitmaskRequestPacket(VcSetEffectBitmaskRequestPacket packet)
    {
        EffectBitmask = packet.Value;
    }

    private void HandleSetPositionBitmaskRequestPacket(VcSetPositionRequestPacket packet)
    {
        Position = packet.Value;
    }

    private void HandleSetRotationBitmaskRequestPacket(VcSetRotationRequestPacket packet)
    {
        Rotation = packet.Value;
    }

    private void HandleSetCaveFactorRequestPacket(VcSetCaveFactorRequest packet)
    {
        CaveFactor = packet.Value;
    }

    private void HandleSetMuffleFactorRequestPacket(VcSetMuffleFactorRequest packet)
    {
        MuffleFactor = packet.Value;
    }

    private void HandleSetTitleRequestPacket(VcSetTitleRequestPacket packet)
    {
        OnSetTitle?.Invoke(packet.Value);
    }

    private void HandleSetDescriptionRequestPacket(VcSetDescriptionRequestPacket packet)
    {
        OnSetDescription?.Invoke(packet.Value);
    }

    private void HandleSetEntityVisibilityRequestPacket(VcSetEntityVisibilityRequestPacket packet)
    {
        var entity = World.GetEntity(packet.Id);
        if (entity is VoiceCraftClientEntity clientEntity)
            clientEntity.IsVisible = packet.Value;
    }

    private void HandleOnEffectUpdatedPacket(VcOnEffectUpdatedPacket packet)
    {
        if (packet.EffectType == EffectType.None)
        {
            AudioEffectSystem.SetEffect(packet.Bitmask, null);
            return;
        }

        AudioEffectSystem.SetEffect(packet.Bitmask, packet.Effect);
    }

    private void HandleOnEntityCreatedPacket(VcOnEntityCreatedPacket packet)
    {
        var entity = new VoiceCraftClientEntity(packet.Id, _audioDecoderFactory.Invoke())
        {
            Name = packet.Name,
            Muted = packet.Muted,
            Deafened = packet.Deafened
        };
        World.AddEntity(entity);
    }

    private void HandleOnNetworkEntityCreatedPacket(VcOnNetworkEntityCreatedPacket packet)
    {
        var entity =
            new VoiceCraftClientNetworkEntity(packet.Id, _audioDecoderFactory.Invoke(), packet.UserGuid)
            {
                Name = packet.Name,
                Muted = packet.Muted,
                Deafened = packet.Deafened,
                ServerMuted = packet.ServerMuted,
                ServerDeafened = packet.ServerDeafened
            };
        World.AddEntity(entity);
    }

    private void HandleOnEntityDestroyedPacket(VcOnEntityDestroyedPacket packet)
    {
        World.DestroyEntity(packet.Id);
    }

    private void HandleOnEntityNameUpdatedPacket(VcOnEntityNameUpdatedPacket packet)
    {
        var entity = World.GetEntity(packet.Id);
        if (entity == null) return;
        entity.Name = packet.Value;
    }

    private void HandleOnEntityMuteUpdatedPacket(VcOnEntityMuteUpdatedPacket packet)
    {
        var entity = World.GetEntity(packet.Id);
        if (entity == null) return;
        entity.Muted = packet.Value;
    }

    private void HandleOnEntityDeafenUpdatedPacket(VcOnEntityDeafenUpdatedPacket packet)
    {
        var entity = World.GetEntity(packet.Id);
        if (entity == null) return;
        entity.Deafened = packet.Value;
    }

    private void HandleOnEntityServerMuteUpdatedPacket(VcOnEntityServerMuteUpdatedPacket packet)
    {
        var entity = World.GetEntity(packet.Id);
        if (entity is not VoiceCraftNetworkEntity networkEntity) return;
        networkEntity.ServerMuted = packet.Value;
    }

    private void HandleOnEntityServerDeafenUpdatedPacket(VcOnEntityServerDeafenUpdatedPacket packet)
    {
        var entity = World.GetEntity(packet.Id);
        if (entity is not VoiceCraftNetworkEntity networkEntity) return;
        networkEntity.ServerDeafened = packet.Value;
    }

    private void HandleOnEntityTalkBitmaskUpdatedPacket(VcOnEntityTalkBitmaskUpdatedPacket packet)
    {
        var entity = World.GetEntity(packet.Id);
        if (entity == null) return;
        entity.TalkBitmask = packet.Value;
    }

    private void HandleOnEntityListenBitmaskUpdatedPacket(VcOnEntityListenBitmaskUpdatedPacket packet)
    {
        var entity = World.GetEntity(packet.Id);
        if (entity == null) return;
        entity.ListenBitmask = packet.Value;
    }

    private void HandleOnEntityEffectBitmaskUpdatedPacket(VcOnEntityEffectBitmaskUpdatedPacket packet)
    {
        var entity = World.GetEntity(packet.Id);
        if (entity == null) return;
        entity.EffectBitmask = packet.Value;
    }

    private void HandleOnEntityPositionUpdatedPacket(VcOnEntityPositionUpdatedPacket packet)
    {
        var entity = World.GetEntity(packet.Id);
        if (entity == null) return;
        entity.Position = packet.Value;
    }

    private void HandleOnEntityRotationUpdatedPacket(VcOnEntityRotationUpdatedPacket packet)
    {
        var entity = World.GetEntity(packet.Id);
        if (entity == null) return;
        entity.Rotation = packet.Value;
    }

    private void HandleOnEntityCaveFactorUpdatedPacket(VcOnEntityCaveFactorUpdatedPacket packet)
    {
        var entity = World.GetEntity(packet.Id);
        if (entity == null) return;
        entity.CaveFactor = packet.Value;
    }

    private void HandleOnEntityMuffleFactorUpdatedPacket(VcOnEntityMuffleFactorUpdatedPacket packet)
    {
        var entity = World.GetEntity(packet.Id);
        if (entity == null) return;
        entity.MuffleFactor = packet.Value;
    }

    private void HandleOnEntityAudioReceivedPacket(VcOnEntityAudioReceivedPacket packet)
    {
        if (Deafened || ServerDeafened) return;
        var entity = World.GetEntity(packet.Id);
        entity?.ReceiveAudio(packet.Buffer, packet.Timestamp, packet.FrameLoudness);
    }

    //Internal Event Handling
    private void OnClientWorldIdUpdated(string worldId, VoiceCraftEntity _)
    {
        if (PositioningType != PositioningType.Client) return;
        SendPacket(PacketPool<VcSetWorldIdRequestPacket>.GetPacket().Set(worldId));
    }

    private void OnClientNameUpdated(string name, VoiceCraftEntity _)
    {
        if (PositioningType != PositioningType.Client) return;
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
        if (PositioningType != PositioningType.Client) return;
        SendPacket(PacketPool<VcSetPositionRequestPacket>.GetPacket().Set(position));
    }

    private void OnClientRotationUpdated(Vector2 rotation, VoiceCraftEntity _)
    {
        if (PositioningType != PositioningType.Client) return;
        SendPacket(PacketPool<VcSetRotationRequestPacket>.GetPacket().Set(rotation));
    }

    private void OnClientCaveFactorUpdated(float caveFactor, VoiceCraftEntity _)
    {
        if (PositioningType != PositioningType.Client) return;
        SendPacket(PacketPool<VcSetCaveFactorRequest>.GetPacket().Set(caveFactor));
    }

    private void OnClientMuffleFactorUpdated(float muffleFactor, VoiceCraftEntity _)
    {
        if (PositioningType != PositioningType.Client) return;
        SendPacket(PacketPool<VcSetMuffleFactorRequest>.GetPacket().Set(muffleFactor));
    }

    private void ProcessPacket<T>(NetDataReader reader, Action<IVoiceCraftPacket> onParsed)
        where T : IVoiceCraftPacket
    {
        var packet = PacketPool<T>.GetPacket();
        try
        {
            packet.Deserialize(reader);
            OnPacketReceived?.Invoke(packet);
            onParsed.Invoke(packet);
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    private void ProcessUnconnectedPacket<T>(NetDataReader reader, Action<IVoiceCraftPacket> onParsed)
        where T : IVoiceCraftPacket
    {
        var packet = PacketPool<T>.GetPacket();
        try
        {
            packet.Deserialize(reader);
            OnUnconnectedPacketReceived?.Invoke(packet);
            onParsed.Invoke(packet);
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }
}