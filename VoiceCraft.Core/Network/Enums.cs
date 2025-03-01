namespace VoiceCraft.Core.Network
{
    public enum PositioningType : byte
    {
        Server,
        Client
    }

    public enum LoginType : byte
    {
        Pinger,
        Login,
        Discovery
    }

    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected
    }

    public enum PacketType : byte
    {
        Login,
        Info,
        Audio,
        SetLocalEntity,
        EntityCreated,
        EntityDestroyed,

        //Components
        AddComponent,
        RemoveComponent,
        UpdateComponent
    }

    public enum ComponentEnum : byte
    {
        AudioListenerComponent,
        AudioSourceComponent,
        AudioStreamComponent,
        TransformComponent,
        
        //Effect Components
        ProximityEffectComponent,
        DirectionalEffectComponent,
    }
}