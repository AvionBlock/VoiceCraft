namespace VoiceCraft.Network;

public enum VcConnectionState : byte
{
    Disconnected,
    Connecting,
    Connected,
    Disconnecting,
    LoginRequested
}

public enum VcDeliveryMethod : byte
{
    Unreliable,
    Reliable
}