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
        SetEffect,
        RemoveEffect,
        //Entity stuff
        EntityCreated,
        EntityDestroyed,
        SetName,
        SetTalkBitmask,
        SetListenBitmask,
        SetPosition,
        SetRotation,
        SetIntProperty,
        SetBoolProperty,
        SetFloatProperty,
        RemoveIntProperty,
        RemoveBoolProperty,
        RemoveFloatProperty,
        
        Unknown //C# does a thing where any number higher than this will always result to this value.
    }

    public enum EffectType : byte
    {
        Proximity,
        Unknown //C# does a thing where any number higher than this will always result to this value.
    }
}