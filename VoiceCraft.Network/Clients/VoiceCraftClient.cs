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
    private ushort _sendTimestamp;

    public static Version Version { get; } = new(Constants.Major, Constants.Minor, Constants.Patch);
    public VoiceCraftWorld World { get; } = new();
    public AudioEffectSystem AudioEffectSystem { get; } = new();
    public abstract PositioningType PositioningType { get; }
    public VcConnectionState ConnectionState { get; protected set; }

    public float InputVolume
    {
        get;
        set => field = ClampFinite(value, 0, 2);
    }

    public float OutputVolume
    {
        get;
        set => field = ClampFinite(value, 0, 2);
    }

    public float MicrophoneSensitivity
    {
        get;
        set => field = ClampFinite(value, 0, 1);
    }

    public bool SpeakingState
    {
        get;
        private set
        {
            if (field == value) return;
            field = value;
            OnSpeakingUpdated?.Invoke(value);
        }
    }

    public bool ServerMuted
    {
        get;
        private set
        {
            if (field == value) return;
            field = value;
            OnServerMuteUpdated?.Invoke(field);
        }
    }

    public bool ServerDeafened
    {
        get;
        private set
        {
            if (field == value) return;
            field = value;
            OnServerDeafenUpdated?.Invoke(field);
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
        return AudioEffectSystem.Read(this, buffer);
    }

    public void Write(Span<float> buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
        {
            var value = buffer[i];
            if (float.IsFinite(value))
                continue;

            buffer[i] = 0f;
        }

        var frameLoudness = SampleLoudness.Read(buffer);
        if (frameLoudness >= MicrophoneSensitivity)
            _lastAudioPeakTime = DateTime.UtcNow;

        _sendTimestamp += 1; //Add to timestamp even though we aren't really connected.
        var shouldDrop = ConnectionState != VcConnectionState.Connected ||
                         ServerMuted ||
                         Muted ||
                         (DateTime.UtcNow - _lastAudioPeakTime).TotalMilliseconds > Constants.SilenceThresholdMs;
        if (shouldDrop)
        {
            SpeakingState = false;
            return;
        }

        SpeakingState = true;
        var encodeBuffer = ArrayPool<byte>.Shared.Rent(Constants.MaximumEncodedBytes);
        var packet = PacketPool<VcAudioRequestPacket>.GetPacket(() => new VcAudioRequestPacket());
        try
        {
            encodeBuffer.AsSpan().Clear();
            var bytesEncoded = _audioEncoder.Encode(
                buffer,
                encodeBuffer.AsSpan(0, Constants.MaximumEncodedBytes),
                Constants.FrameSize);
            switch (bytesEncoded)
            {
                case <= 0:
                case > Constants.MaximumEncodedBytes:
                    return;
                default:
                    packet.Set(_sendTimestamp, frameLoudness, bytesEncoded, encodeBuffer);
                    SendPacket(packet);
                    break;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(encodeBuffer);
            packet.Return();
        }
    }

    public override void Reset()
    {
        //Doesn't remove the entity from the world.
        Name = "New Client";
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
        //We clear both for the reset.
        ClearProperties();
        ClearVisibleEntities();
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
                ProcessPacket(reader, onParsed, () => new VcLogoutRequestPacket());
                break;
            case VcPacketType.InfoResponse:
                ProcessPacket(reader, onParsed, () => new VcInfoResponsePacket());
                break;
            case VcPacketType.AcceptResponse:
                ProcessPacket(reader, onParsed, () => new VcAcceptResponsePacket());
                break;
            case VcPacketType.DenyResponse:
                ProcessPacket(reader, onParsed, () => new VcDenyResponsePacket());
                break;
            case VcPacketType.EventRequest:
                ProcessPacket(reader, onParsed, () => new VcEventRequestPacket());
                break;
            case VcPacketType.SetNameRequest:
                ProcessPacket(reader, onParsed, () => new VcSetNameRequestPacket());
                break;
            case VcPacketType.SetServerMuteRequest:
                ProcessPacket(reader, onParsed, () => new VcSetServerMuteRequestPacket());
                break;
            case VcPacketType.SetServerDeafenRequest:
                ProcessPacket(reader, onParsed, () => new VcSetServerDeafenRequestPacket());
                break;
            case VcPacketType.SetWorldIdRequest:
                ProcessPacket(reader, onParsed, () => new VcSetWorldIdRequestPacket());
                break;
            case VcPacketType.SetTalkBitmaskRequest:
                ProcessPacket(reader, onParsed, () => new VcSetTalkBitmaskRequestPacket());
                break;
            case VcPacketType.SetListenBitmaskRequest:
                ProcessPacket(reader, onParsed, () => new VcSetListenBitmaskRequestPacket());
                break;
            case VcPacketType.SetEffectBitmaskRequest:
                ProcessPacket(reader, onParsed, () => new VcSetEffectBitmaskRequestPacket());
                break;
            case VcPacketType.SetPositionRequest:
                ProcessPacket(reader, onParsed, () => new VcSetPositionRequestPacket());
                break;
            case VcPacketType.SetRotationRequest:
                ProcessPacket(reader, onParsed, () => new VcSetRotationRequestPacket());
                break;
            case VcPacketType.SetPropertyRequest:
                ProcessPacket(reader, onParsed, () => new VcSetPropertyRequestPacket());
                break;
            case VcPacketType.SetTitleRequest:
                ProcessPacket(reader, onParsed, () => new VcSetTitleRequestPacket());
                break;
            case VcPacketType.SetDescriptionRequest:
                ProcessPacket(reader, onParsed, () => new VcSetDescriptionRequestPacket());
                break;
            case VcPacketType.SetEntityVisibilityRequest:
                ProcessPacket(reader, onParsed, () => new VcSetEntityVisibilityRequestPacket());
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
            case VcPacketType.InfoResponse:
                ProcessUnconnectedPacket(reader, onParsed, () => new VcInfoResponsePacket());
                break;
            case VcPacketType.InfoRequest:
            case VcPacketType.LoginRequest:
            case VcPacketType.LogoutRequest:
            case VcPacketType.AcceptResponse:
            case VcPacketType.DenyResponse:
            case VcPacketType.EventRequest:
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
            case VcPacketType.SetPropertyRequest:
            case VcPacketType.SetTitleRequest:
            case VcPacketType.SetDescriptionRequest:
            case VcPacketType.SetEntityVisibilityRequest:
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
            case VcEventRequestPacket eventRequestPacket:
                HandleEventRequestPacket(eventRequestPacket);
                break;
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
                HandleSetPositionRequestPacket(setPositionRequestPacket);
                break;
            case VcSetRotationRequestPacket setRotationRequestPacket:
                HandleSetRotationRequestPacket(setRotationRequestPacket);
                break;
            case VcSetPropertyRequestPacket setPropertyRequestPacket:
                HandleSetPropertyRequestPacket(setPropertyRequestPacket);
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

            OnSetTitle = null;
            OnSetDescription = null;
            OnSpeakingUpdated = null;
            OnServerMuteUpdated = null;
            OnServerDeafenUpdated = null;
        }

        Disposed = true;
    }

    //Packet Handling
    private void HandleEventRequestPacket(VcEventRequestPacket packet)
    {
        switch (packet.Event)
        {
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
            case VcOnEntityPropertyUpdatedPacket onEntityPropertyUpdatedPacket:
                HandleOnEntityPropertyUpdatedPacket(onEntityPropertyUpdatedPacket);
                break;
            case VcOnEntityAudioDataReceivedPacket onEntityAudioDataReceivedPacket:
                HandleOnEntityAudioDataReceivedPacket(onEntityAudioDataReceivedPacket);
                break;
        }
    }

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

    private void HandleSetPositionRequestPacket(VcSetPositionRequestPacket packet)
    {
        Position = packet.Value;
    }

    private void HandleSetRotationRequestPacket(VcSetRotationRequestPacket packet)
    {
        Rotation = packet.Value;
    }

    private void HandleSetPropertyRequestPacket(VcSetPropertyRequestPacket packet)
    {
        SetProperty(packet.Key, packet.Value);
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

    //Events
    private void HandleOnEffectUpdatedPacket(VcOnEffectUpdatedPacket packet)
    {
        AudioEffectSystem.SetEffect(packet.Bitmask, packet.Effect);
    }

    private void HandleOnEntityCreatedPacket(VcOnEntityCreatedPacket packet)
    {
        if (World.ContainsEntity(packet.Id)) return;

        var entity = new VoiceCraftClientEntity(packet.Id, _audioDecoderFactory.Invoke());
        World.AddEntity(entity);
    }

    private void HandleOnNetworkEntityCreatedPacket(VcOnNetworkEntityCreatedPacket packet)
    {
        if (World.ContainsEntity(packet.Id)) return;

        var entity = new VoiceCraftClientNetworkEntity(packet.Id, _audioDecoderFactory.Invoke(), packet.UserGuid);
        World.AddEntity(entity);
    }

    private void HandleOnEntityDestroyedPacket(VcOnEntityDestroyedPacket packet)
    {
        if (!World.ContainsEntity(packet.Id)) return;
        World.DestroyEntity(packet.Id);
    }

    private void HandleOnEntityNameUpdatedPacket(VcOnEntityNameUpdatedPacket packet)
    {
        var entity = World.GetEntity(packet.Id);
        entity?.Name = packet.Value;
    }

    private void HandleOnEntityMuteUpdatedPacket(VcOnEntityMuteUpdatedPacket packet)
    {
        var entity = World.GetEntity(packet.Id);
        entity?.Muted = packet.Value;
    }

    private void HandleOnEntityDeafenUpdatedPacket(VcOnEntityDeafenUpdatedPacket packet)
    {
        var entity = World.GetEntity(packet.Id);
        entity?.Deafened = packet.Value;
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
        entity?.TalkBitmask = packet.Value;
    }

    private void HandleOnEntityListenBitmaskUpdatedPacket(VcOnEntityListenBitmaskUpdatedPacket packet)
    {
        var entity = World.GetEntity(packet.Id);
        entity?.ListenBitmask = packet.Value;
    }

    private void HandleOnEntityEffectBitmaskUpdatedPacket(VcOnEntityEffectBitmaskUpdatedPacket packet)
    {
        var entity = World.GetEntity(packet.Id);
        entity?.EffectBitmask = packet.Value;
    }

    private void HandleOnEntityPositionUpdatedPacket(VcOnEntityPositionUpdatedPacket packet)
    {
        var entity = World.GetEntity(packet.Id);
        entity?.Position = packet.Value;
    }

    private void HandleOnEntityRotationUpdatedPacket(VcOnEntityRotationUpdatedPacket packet)
    {
        var entity = World.GetEntity(packet.Id);
        entity?.Rotation = packet.Value;
    }

    private void HandleOnEntityPropertyUpdatedPacket(VcOnEntityPropertyUpdatedPacket packet)
    {
        var entity = World.GetEntity(packet.Id);
        entity?.SetProperty(packet.Key, packet.Value);
    }

    private void HandleOnEntityAudioDataReceivedPacket(VcOnEntityAudioDataReceivedPacket packet)
    {
        if (Deafened || ServerDeafened) return;
        var entity = World.GetEntity(packet.Id);
        entity?.ReceiveAudio(packet.Buffer, packet.Timestamp, packet.FrameLoudness);
    }

    //Internal Event Handling
    private void OnClientWorldIdUpdated(string worldId, VoiceCraftEntity _)
    {
        if (PositioningType != PositioningType.Client) return;
        var packet = PacketPool<VcSetWorldIdRequestPacket>.GetPacket(() => new VcSetWorldIdRequestPacket());
        try
        {
            packet.Set(worldId);
            SendPacket(packet);
        }
        finally
        {
            packet.Return();
        }
    }

    private void OnClientNameUpdated(string name, VoiceCraftEntity _)
    {
        if (PositioningType != PositioningType.Client) return;
        var packet = PacketPool<VcSetNameRequestPacket>.GetPacket(() => new VcSetNameRequestPacket());
        try
        {
            packet.Set(name);
            SendPacket(packet);
        }
        finally
        {
            packet.Return();
        }
    }

    private void OnClientMuteUpdated(bool value, VoiceCraftEntity _)
    {
        var packet = PacketPool<VcSetMuteRequestPacket>.GetPacket(() => new VcSetMuteRequestPacket());
        try
        {
            packet.Set(value);
            SendPacket(packet);
        }
        finally
        {
            packet.Return();
        }
    }

    private void OnClientDeafenUpdated(bool value, VoiceCraftEntity _)
    {
        var packet = PacketPool<VcSetDeafenRequestPacket>.GetPacket(() => new VcSetDeafenRequestPacket());
        try
        {
            packet.Set(value);
            SendPacket(packet);
        }
        finally
        {
            packet.Return();
        }
    }

    private void OnClientPositionUpdated(Vector3 position, VoiceCraftEntity _)
    {
        if (PositioningType != PositioningType.Client) return;
        var packet = PacketPool<VcSetPositionRequestPacket>.GetPacket(() => new VcSetPositionRequestPacket());
        try
        {
            packet.Set(position);
            SendPacket(packet);
        }
        finally
        {
            packet.Return();
        }
    }

    private void OnClientRotationUpdated(Vector2 rotation, VoiceCraftEntity _)
    {
        if (PositioningType != PositioningType.Client) return;
        var packet = PacketPool<VcSetRotationRequestPacket>.GetPacket(() => new VcSetRotationRequestPacket());
        try
        {
            packet.Set(rotation);
            SendPacket(packet);
        }
        finally
        {
            packet.Return();
        }
    }

    private void ProcessPacket<T>(NetDataReader reader, Action<IVoiceCraftPacket> onParsed, Func<T> packetFactory)
        where T : IVoiceCraftPacket
    {
        var packet = PacketPool<T>.GetPacket(packetFactory);
        try
        {
            packet.Deserialize(reader);
            OnPacketReceived?.Invoke(packet);
            onParsed.Invoke(packet);
        }
        finally
        {
            packet.Return();
        }
    }

    private void ProcessUnconnectedPacket<T>(NetDataReader reader, Action<IVoiceCraftPacket> onParsed,
        Func<T> packetFactory)
        where T : IVoiceCraftPacket
    {
        var packet = PacketPool<T>.GetPacket(packetFactory);
        try
        {
            packet.Deserialize(reader);
            OnUnconnectedPacketReceived?.Invoke(packet);
            onParsed.Invoke(packet);
        }
        finally
        {
            packet.Return();
        }
    }

    private static float ClampFinite(float value, float min, float max)
    {
        return float.IsFinite(value) ? Math.Clamp(value, min, max) : min;
    }
}