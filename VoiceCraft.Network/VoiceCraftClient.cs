using System;
using System.Numerics;
using System.Threading.Tasks;
using VoiceCraft.Core;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Interfaces;
using VoiceCraft.Network.NetPeers;
using VoiceCraft.Network.Packets.VcPackets;
using VoiceCraft.Network.Packets.VcPackets.Event;
using VoiceCraft.Network.Packets.VcPackets.Request;
using VoiceCraft.Network.Packets.VcPackets.Response;
using VoiceCraft.Network.Systems;
using VoiceCraft.Network.World;

namespace VoiceCraft.Network;

public class VoiceCraftClient : VoiceCraftEntity, IDisposable
{
    public static readonly Version Version = new(Constants.Major, Constants.Minor, Constants.Patch);
    private readonly byte[] _encodeBuffer = new byte[Constants.MaximumEncodedBytes];
    private readonly AudioEffectSystem _audioEffectSystem = new();
    private readonly VcNetworkBackend _networkBackend;
    private VoiceCraftNetPeer? _serverPeer;
    private bool _disposed;

    //Audio
    private DateTime _lastAudioPeakTime = DateTime.MinValue;
    private float _microphoneSensitivity;
    private float _outputVolume;
    private ushort _sendTimestamp;
    private bool _serverDeafened;
    private bool _serverMuted;

    //Properties
    public VcConnectionState ConnectionState => _serverPeer?.ConnectionState ?? VcConnectionState.Disconnected;
    public PositioningType PositioningType => _serverPeer?.PositioningType ?? PositioningType.Server;

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

    //Events
    public event Action? OnConnected;
    public event Action<string>? OnDisconnected;
    public event Action<string>? OnSetTitle;
    public event Action<string>? OnSetDescription;
    public event Action<bool>? OnSpeakingUpdated;
    public event Action<bool>? OnServerMuteUpdated;
    public event Action<bool>? OnServerDeafenUpdated;

    public VoiceCraftClient(VcNetworkBackend networkBackend) : base(0, new VoiceCraftWorld())
    {
        _networkBackend = networkBackend;
        _networkBackend.OnNetworkReceive += NetworkBackendOnNetworkReceive;

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

    public async Task<ServerInfo> PingAsync(string ip, int port)
    {
        _networkBackend.SendUnconnectedPacket(ip, port,
            PacketPool<VcInfoRequestPacket>.GetPacket().Set(Environment.TickCount));
        var result = await _networkBackend.GetUnconnectedResponseAsync<VcInfoResponsePacket>(TimeSpan.FromSeconds(10));
        return new ServerInfo(result);
    }

    public async Task<bool> ConnectAsync(string ip, int port, Guid userGuid, Guid serverUserGuid, string locale,
        PositioningType positioningType)
    {
        if (ConnectionState != VcConnectionState.Disconnected) return false;
        await Task.Run(() => _networkBackend.Start());
        _serverPeer = await Task.Run(() =>
            _networkBackend.Connect(ip, port, userGuid, serverUserGuid, locale, positioningType));
        OnConnected?.Invoke();
        return true;
    }

    public void Update()
    {
        if (ConnectionState == VcConnectionState.Disconnected || !_networkBackend.IsStarted) return;
        _networkBackend.Update();
    }

    public async Task<bool> DisconnectAsync(string? reason = null)
    {
        if (ConnectionState == VcConnectionState.Disconnected || !_networkBackend.IsStarted ||
            _serverPeer == null) return false;
        await Task.Run(() =>
        {
            _networkBackend.Disconnect(_serverPeer, reason);
            _networkBackend.Stop();
        });

        OnDisconnected?.Invoke(reason ?? "");
        return true;
    }

    public void SendPacket<T>(T packet, VcDeliveryMethod deliveryMethod = VcDeliveryMethod.Reliable)
        where T : IVoiceCraftPacket
    {
        if (!_networkBackend.IsStarted || _serverPeer == null) return;
        try
        {
            _networkBackend.SendPacket(_serverPeer, packet, deliveryMethod);
        }
        finally
        {
            PacketPool<T>.Return(packet);
        }
    }

    //Write Encoded Bytes
    public void SendAudio(Span<byte> buffer, int frameLoudness)
    {
        if (frameLoudness >= MicrophoneSensitivity)
            _lastAudioPeakTime = DateTime.UtcNow;

        _sendTimestamp += 1; //Add to timestamp even though we aren't really connected.
        if ((DateTime.UtcNow - _lastAudioPeakTime).TotalMilliseconds > Constants.SilenceThresholdMs ||
            _serverPeer == null ||
            ConnectionState != VcConnectionState.Connected || Muted || ServerMuted) return;

        lock (_encodeBuffer)
        {
            if (!buffer.TryCopyTo(_encodeBuffer)) return;
            SendPacket(PacketPool<VcAudioRequestPacket>.GetPacket()
                .Set(_sendTimestamp, frameLoudness, buffer.Length, _encodeBuffer));
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            World.Dispose();
            _networkBackend.Dispose();

            _networkBackend.OnNetworkReceive -= NetworkBackendOnNetworkReceive;
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
            OnSetTitle = null;
            OnSetDescription = null;
            OnSpeakingUpdated = null;
            OnServerMuteUpdated = null;
            OnServerDeafenUpdated = null;
        }

        _disposed = true;
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

    //Packet Handling
    private void NetworkBackendOnNetworkReceive(VoiceCraftNetPeer netPeer, IVoiceCraftPacket packet)
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
            _audioEffectSystem.SetEffect(packet.Bitmask, null);
            return;
        }

        _audioEffectSystem.SetEffect(packet.Bitmask, packet.Effect);
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
        var netPeer = new VoiceCraftClientNetPeer(Version, packet.UserGuid, Guid.Empty, Constants.DefaultLanguage,
            PositioningType.Server);
        var entity = new VoiceCraftNetworkEntity(netPeer, packet.Id, World)
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
        entity?.ReceiveAudio(packet.Data, packet.Timestamp, packet.FrameLoudness);
    }
}