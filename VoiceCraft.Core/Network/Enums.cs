namespace VoiceCraft.Core.Network.Packets
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
        ServerInfo,
        EntityCreated,
        EntityRemoved,
        EntityAudio,
    }
}