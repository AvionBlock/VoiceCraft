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
        Discovery
    }

    public enum PacketType : byte
    {
        Info,
        Login,
        Audio,
        //Entity stuff
        EntityCreated,
        EntityDestroyed,
        UpdatePosition,
        UpdateRotation,
        UpdateTalkBitmask,
        UpdateListenBitmask,
        UpdateName,
        AddEffect,
        UpdateEffect,
        RemoveEffect,
        
        Unknown //C# does a thing where any number higher than this will always result to this value.
    }

    public enum EffectType : byte
    {
        Proximity,
        Unknown //C# does a thing where any number higher than this will always result to this value.
    }
}