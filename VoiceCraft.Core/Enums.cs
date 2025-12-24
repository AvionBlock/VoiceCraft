namespace VoiceCraft.Core
{
    #region Network

    public enum PositioningType : byte
    {
        Server,
        Client
    }

    public enum VcPacketType : byte
    {
        //Core
        //Requests DO NOT CHANGE!
        InfoRequest,
        LoginRequest,
        LogoutRequest,
        //Responses DO NOT CHANGE!
        InfoResponse,
        AcceptResponse,
        DenyResponse,
        
        //Other/Changeable
        //Requests
        SetNameRequest,
        AudioRequest,
        SetMuteRequest,
        SetDeafenRequest,
        SetWorldIdRequest,
        SetTalkBitmaskRequest,
        SetListenBitmaskRequest,
        SetEffectBitmaskRequest,
        SetPositionRequest,
        SetRotationRequest,
        SetCaveFactorRequest,
        SetMuffleFactorRequest,
        SetTitleRequest,
        SetDescriptionRequest,
        SetEntityVisibilityRequest,

        //Responses

        //Events
        OnEffectUpdated,
        OnEntityCreated,
        OnNetworkEntityCreated,
        OnEntityDestroyed,
        OnEntityNameUpdated,
        OnEntityMuteUpdated,
        OnEntityDeafenUpdated,
        OnEntityTalkBitmaskUpdated,
        OnEntityListenBitmaskUpdated,
        OnEntityEffectBitmaskUpdated,
        OnEntityPositionUpdated,
        OnEntityRotationUpdated,
        OnEntityCaveFactorUpdated,
        OnEntityMuffleFactorUpdated,
        OnEntityAudioReceived,
    }

    public enum McApiPacketType : byte
    {
        //Core
        //Requests DO NOT CHANGE!
        LoginRequest,
        LogoutRequest,
        PingRequest,
        //Responses DO NOT CHANGE!
        AcceptResponse,
        DenyResponse,
        PingResponse,
        
        //Other/Changeable
        //Requests
        SetEffectRequest,
        ClearEffectsRequest,
        SetEntityTitleRequest,
        SetEntityDescriptionRequest,
        SetEntityWorldIdRequest,
        SetEntityNameRequest,
        SetEntityTalkBitmaskRequest,
        SetEntityListenBitmaskRequest,
        SetEntityEffectBitmaskRequest,
        SetEntityPositionRequest,
        SetEntityRotationRequest,
        SetEntityCaveFactorRequest,
        SetEntityMuffleFactorRequest,
        
        //Responses

        //Events
        OnEffectUpdated,
        OnEntityCreated,
        OnNetworkEntityCreated,
        OnEntityDestroyed,
        OnEntityVisibilityUpdated,
        OnEntityWorldIdUpdated,
        OnEntityNameUpdated,
        OnEntityMuteUpdated,
        OnEntityDeafenUpdated,
        OnEntityTalkBitmaskUpdated,
        OnEntityListenBitmaskUpdated,
        OnEntityEffectBitmaskUpdated,
        OnEntityPositionUpdated,
        OnEntityRotationUpdated,
        OnEntityCaveFactorUpdated,
        OnEntityMuffleFactorUpdated,
        OnEntityAudioReceived
    }

    public enum VcConnectionState : byte
    {
        Disconnected,
        Connecting,
        Connected,
        Disconnecting,
    }

    #endregion

    #region Audio

    public enum EffectType : byte
    {
        None,
        Visibility,
        Proximity,
        Directional,
        ProximityEcho,
        Echo
    }

    public enum AudioFormat
    {
        Pcm8,
        Pcm16,
        PcmFloat
    }

    public enum CaptureState
    {
        Stopped,
        Starting,
        Capturing,
        Stopping
    }

    public enum PlaybackState
    {
        Stopped,
        Starting,
        Playing,
        Paused,
        Stopping
    }

    #endregion

    #region Other

    public enum BackgroundProcessStatus
    {
        Stopped,
        Started,
        Completed,
        Error
    }

    #endregion
}