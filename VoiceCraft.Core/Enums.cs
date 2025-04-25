namespace VoiceCraft.Core
{
    #region Network
    
    public enum PositioningType : byte
    {
        Server,
        Client
    }

    public enum LoginType : byte
    {
        Login,
        Discovery,
        Unknown
    }

    public enum PacketType : byte
    {
        Info,
        Login,
        Audio,
        SetTitle,
        SetDescription,
        SetEffect,
        RemoveEffect,

        //Entity stuff
        EntityCreated,
        EntityDestroyed,
        SetVisibility,
        SetName,
        SetTalkBitmask,
        SetListenBitmask,
        SetMinRange,
        SetMaxRange,
        SetPosition,
        SetRotation,
        SetProperty,
        RemoveProperty,

        Unknown
    }
    
    #endregion

    #region Properties
    
    public enum PropertyKey : ushort
    {
        Unknown
    }

    public enum PropertyType : byte
    {
        Byte,
        Int,
        UInt,
        Float,
        Unknown
    }
    
    #endregion

    #region Audio

    public enum EffectType : byte
    {
        Unknown
    }

    public enum AudioFormat
    {
        Pcm8,
        Pcm16,
        PcmFloat,
    }

    public enum CaptureState
    {
        Stopped,
        Starting,
        Capturing,
        Stopping,
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

    public enum Bitmasks : ulong
    {
        ProximityEffect = 0b0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0001,
        DirectionalEffect = 0b0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0010
    }
    
    public enum BackgroundProcessStatus
    {
        Stopped,
        Started,
        Completed,
        Error
    }

    #endregion
}