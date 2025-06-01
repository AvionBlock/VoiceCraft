namespace VoiceCraft.Core
{
    #region Network

    public enum PositioningType : byte
    {
        Unknown,
        Server,
        Client
    }

    public enum LoginType : byte
    {
        Unknown,
        Login,
        Discovery
    }

    public enum EntityType : byte
    {
        Unknown,
        Server,
        Network
    }

    public enum PacketType : byte
    {
        Unknown,
        Info,
        Login,
        Audio,
        SetTitle,
        SetDescription,
        SetEffect,

        //Entity stuff
        EntityCreated,
        EntityDestroyed,
        SetVisibility,
        SetName,
        SetMute,
        SetDeafen,
        SetTalkBitmask,
        SetListenBitmask,
        SetPosition,
        SetRotation,
        SetProperty
    }

    public enum McLinkPacketType : byte
    {
        Unknown,
        Login,
        Logout,
        Ping
    }

    #endregion

    #region Properties

    public enum PropertyKey : ushort
    {
        Unknown,
        ProximityEffectMinRange,
        ProximityEffectMaxRange
    }

    public enum PropertyType : byte
    {
        Null,
        Byte,
        Int,
        UInt,
        Float
    }

    #endregion

    #region Audio

    public enum EffectType : byte
    {
        Unknown,
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