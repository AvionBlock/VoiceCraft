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
        EntityCreated,
        EntityDestroyed,
        Unknown
    }
}