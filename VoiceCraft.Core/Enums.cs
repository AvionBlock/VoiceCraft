namespace VoiceCraft.Core
{
    public enum EffectBitmask : ulong
    {
        ProximityEffect = 0b0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0001,
        DirectionalEffect = 0b0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0010
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
    
    public enum BackgroundProcessStatus
    {
        Stopped,
        Started,
        Completed,
        Error
    }
}