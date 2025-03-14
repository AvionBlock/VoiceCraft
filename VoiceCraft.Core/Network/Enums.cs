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
        Info,
        Login,
        
        //ECS
        EntityCreated,
        AddComponent,
        UpdateComponent,
        RemoveComponent,
        EntityDestroyed,
    }

    public enum ComponentType : byte
    {
        //Audio
        AudioListener,
        AudioSource,
        AudioStream,
        AudioEffects,
        
        //Streamable
        Microphone,
        Speaker,
        
        //Other
        Transform,
        Unknown = 255
    }
}