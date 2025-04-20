namespace VoiceCraft.Core.Network
{
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
        SetEffect,
        RemoveEffect,
        //Entity stuff
        EntityCreated,
        EntityReset,
        EntityDestroyed,
        SetName,
        SetTalkBitmask,
        SetListenBitmask,
        SetPosition,
        SetRotation,
        SetProperty,
        RemoveProperty,
        
        Unknown
    }

    public enum EffectType : byte
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
}