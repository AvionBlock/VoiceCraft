namespace VoiceCraft.Core
{
    #region Network

    public enum PositioningType : byte
    {
        Unknown,
        Server,
        Client
    }

    public enum PacketType : byte
    {
        Unknown,
        Info,
        Login,
        Logout,
        SetId,
        SetEffect,
        
        //Client Entity Stuff
        Audio,
        SetTitle,
        SetDescription,

        //Entity stuff
        EntityCreated,
        NetworkEntityCreated,
        EntityDestroyed,
        SetVisibility,
        SetName,
        SetMute,
        SetDeafen,
        SetTalkBitmask,
        SetListenBitmask,
        SetEffectBitmask,
        SetPosition,
        SetRotation
    }

    public enum McApiPacketType : byte
    {
        Unknown,
        Login,
        Logout,
        Ping,
        Accept,
        Deny,
        
        //Server Stuff
        SetEffect,
        
        //Client Entity Stuff
        Audio,
        SetTitle,
        SetDescription,
        
        //Entity stuff
        EntityCreated,
        EntityDestroyed,
        SetName,
        SetMute,
        SetDeafen,
        SetTalkBitmask,
        SetListenBitmask,
        SetPosition,
        SetRotation
    }

    #endregion

    #region Audio

    public enum EffectType : byte
    {
        Unknown,
        Visibility,
        Proximity
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