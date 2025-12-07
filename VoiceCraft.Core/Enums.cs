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
        //Requests
        InfoRequest,
        LoginRequest,
        LogoutRequest,
        AudioRequest,
        SetMuteRequest,
        SetDeafenRequest,
        SetTitleRequest,
        SetDescriptionRequest,
        SetEntityVisibilityRequest,
        
        //Responses
        AcceptResponse,
        DenyResponse,
        
        //Internal Responses
        SetIdAcceptResponse,

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
        OnEntityAudioUpdated,
    }

    public enum McApiPacketType : byte
    {
        //Requests
        LoginRequest,
        LogoutRequest,
        PingRequest,
        
        //Responses
        AcceptResponse,
        DenyResponse,
        
        //Events
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