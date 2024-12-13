namespace VoiceCraft.Core.Network
{
    public enum ConnectionType
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
}