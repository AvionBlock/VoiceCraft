using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib.Utils;
using VoiceCraft.Core;
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
    private readonly Func<IAudioEncoder> _audioEncoderFactory;
    private readonly Func<IAudioDecoder> _audioDecoderFactory;
    private DateTime _lastAudioPeakTime = DateTime.MinValue;
    private float _microphoneSensitivity;
    private float _outputVolume;
    private ushort _sendTimestamp;
    private bool _serverDeafened;
    private bool _serverMuted;
    private bool _speakingState;

    public static Version Version { get; } = new(Constants.Major, Constants.Minor, Constants.Patch);
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

    public abstract event Action? OnConnected;
    public abstract event Action<string?>? OnDisconnected;
    public event Action<string>? OnSetTitle;
    public event Action<string>? OnSetDescription;
    public event Action<bool>? OnSpeakingUpdated;
    public event Action<bool>? OnServerMuteUpdated;
    public event Action<bool>? OnServerDeafenUpdated;

    public VoiceCraftClient(Func<IAudioEncoder> encoderFactory, Func<IAudioDecoder> decoderFactory) : base(0,
        new VoiceCraftWorld())
    {
        _audioEncoderFactory = encoderFactory;
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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected static IVoiceCraftPacket? ProcessPacket(NetDataReader reader)
    {
        try
        {
            var packetType = (VcPacketType)reader.GetByte();
            IVoiceCraftPacket? packet;
            switch (packetType)
            {
                case VcPacketType.LogoutRequest:
                    packet = ProcessPacket<VcLogoutRequestPacket>(reader);
                    break;
                case VcPacketType.InfoResponse:
                    packet = ProcessPacket<VcInfoResponsePacket>(reader);
                    break;
                case VcPacketType.AcceptResponse:
                    packet = ProcessPacket<VcAcceptResponsePacket>(reader);
                    break;
                case VcPacketType.DenyResponse:
                    packet = ProcessPacket<VcDenyResponsePacket>(reader);
                    break;
                case VcPacketType.SetNameRequest:
                    packet = ProcessPacket<VcSetNameRequestPacket>(reader);
                    break;
                case VcPacketType.SetServerMuteRequest:
                    packet = ProcessPacket<VcSetServerMuteRequestPacket>(reader);
                    break;
                case VcPacketType.SetServerDeafenRequest:
                    packet = ProcessPacket<VcSetServerDeafenRequestPacket>(reader);
                    break;
                case VcPacketType.SetWorldIdRequest:
                    packet = ProcessPacket<VcSetWorldIdRequestPacket>(reader);
                    break;
                case VcPacketType.SetTalkBitmaskRequest:
                    packet = ProcessPacket<VcSetTalkBitmaskRequestPacket>(reader);
                    break;
                case VcPacketType.SetListenBitmaskRequest:
                    packet = ProcessPacket<VcSetListenBitmaskRequestPacket>(reader);
                    break;
                case VcPacketType.SetEffectBitmaskRequest:
                    packet = ProcessPacket<VcSetEffectBitmaskRequestPacket>(reader);
                    break;
                case VcPacketType.SetPositionRequest:
                    packet = ProcessPacket<VcSetPositionRequestPacket>(reader);
                    break;
                case VcPacketType.SetRotationRequest:
                    packet = ProcessPacket<VcSetRotationRequestPacket>(reader);
                    break;
                case VcPacketType.SetCaveFactorRequest:
                    packet = ProcessPacket<VcSetCaveFactorRequest>(reader);
                    break;
                case VcPacketType.SetMuffleFactorRequest:
                    packet = ProcessPacket<VcSetMuffleFactorRequest>(reader);
                    break;
                case VcPacketType.SetTitleRequest:
                    packet = ProcessPacket<VcSetTitleRequestPacket>(reader);
                    break;
                case VcPacketType.SetDescriptionRequest:
                    packet = ProcessPacket<VcSetDescriptionRequestPacket>(reader);
                    break;
                case VcPacketType.SetEntityVisibilityRequest:
                    packet = ProcessPacket<VcSetEntityVisibilityRequestPacket>(reader);
                    break;
                case VcPacketType.OnEffectUpdated:
                    packet = ProcessPacket<VcOnEffectUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityCreated:
                    packet = ProcessPacket<VcOnEntityCreatedPacket>(reader);
                    break;
                case VcPacketType.OnNetworkEntityCreated:
                    packet = ProcessPacket<VcOnNetworkEntityCreatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityDestroyed:
                    packet = ProcessPacket<VcOnEntityDestroyedPacket>(reader);
                    break;
                case VcPacketType.OnEntityNameUpdated:
                    packet = ProcessPacket<VcOnEntityNameUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityMuteUpdated:
                    packet = ProcessPacket<VcOnEntityMuteUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityDeafenUpdated:
                    packet = ProcessPacket<VcOnEntityDeafenUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityServerMuteUpdated:
                    packet = ProcessPacket<VcOnEntityServerMuteUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityServerDeafenUpdated:
                    packet = ProcessPacket<VcOnEntityServerDeafenUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityTalkBitmaskUpdated:
                    packet = ProcessPacket<VcOnEntityTalkBitmaskUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityListenBitmaskUpdated:
                    packet = ProcessPacket<VcOnEntityListenBitmaskUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityEffectBitmaskUpdated:
                    packet = ProcessPacket<VcOnEntityEffectBitmaskUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityPositionUpdated:
                    packet = ProcessPacket<VcOnEntityPositionUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityRotationUpdated:
                    packet = ProcessPacket<VcOnEntityRotationUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityCaveFactorUpdated:
                    packet = ProcessPacket<VcOnEntityCaveFactorUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityMuffleFactorUpdated:
                    packet = ProcessPacket<VcOnEntityMuffleFactorUpdatedPacket>(reader);
                    break;
                case VcPacketType.OnEntityAudioReceived:
                    packet = ProcessPacket<VcOnEntityAudioReceivedPacket>(reader);
                    break;
                case VcPacketType.InfoRequest:
                case VcPacketType.LoginRequest:
                case VcPacketType.AudioRequest:
                case VcPacketType.SetMuteRequest:
                case VcPacketType.SetDeafenRequest:
                default:
                    return null;
            }

            return packet;
        }
        catch
        {
            return null;
        }
    }

    protected static IVoiceCraftPacket? ProcessUnconnectedPacket(NetDataReader reader)
    {
        try
        {
            var packetType = (VcPacketType)reader.GetByte();
            IVoiceCraftPacket? packet;
            switch (packetType)
            {
                case VcPacketType.InfoRequest:
                    packet = ProcessPacket<VcInfoRequestPacket>(reader);
                    break;
                case VcPacketType.LoginRequest:
                case VcPacketType.LogoutRequest:
                    return null;
                case VcPacketType.InfoResponse:
                    packet = ProcessPacket<VcInfoResponsePacket>(reader);
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
                    return null;
            }

            return packet;
        }
        catch
        {
            return null;
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
            World.Dispose();
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
        if (entity == null) return;
        if (packet.Value)
        {
            AddVisibleEntity(entity);
            return;
        }

        RemoveVisibleEntity(entity);
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
        var entity = new VoiceCraftEntity(packet.Id, World)
        {
            Name = packet.Name,
            Muted = packet.Muted,
            Deafened = packet.Deafened
        };
        World.AddEntity(entity);
    }

    private void HandleOnNetworkEntityCreatedPacket(VcOnNetworkEntityCreatedPacket packet)
    {
        var entity = new VoiceCraftClientNetworkEntity(packet.Id, World, _audioDecoderFactory.Invoke(), packet.UserGuid)
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

    private static IVoiceCraftPacket ProcessPacket<T>(NetDataReader reader) where T : IVoiceCraftPacket
    {
        var packet = PacketPool<T>.GetPacket();
        packet.Deserialize(reader);
        return packet;
    }
}