namespace VoiceCraft.Core
{
    #region Network

    public enum PositioningType : byte
    {
        Server,
        Client
    }

    public enum PacketType : byte
    {
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
        SetRotation,
        SetCaveFactor,
        SetMuffleFactor
    }

    public enum McApiPacketType : byte
    {
        //Networking
        Login,
        Logout,
        Ping,
        Accept,
        Deny,
        
        //Entity Stuff
        EntityCreated,
        NetworkEntityCreated,
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