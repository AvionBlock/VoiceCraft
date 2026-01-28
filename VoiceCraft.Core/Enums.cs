namespace VoiceCraft.Core
{
    #region Audio

    public enum AudioFormat
    {
        Pcm8,
        Pcm16,
        PcmFloat
    }

    public enum CaptureState
    {
        Stopped,
        Starting,
        Capturing,
        Stopping
    }

    public enum PlaybackState
    {
        Stopped,
        Starting,
        Playing,
        Paused,
        Stopping
    }

    #endregion

    #region Other

    public enum BackgroundProcessStatus
    {
        Stopped,
        Started,
        Completed,
        Error
    }

    #endregion
}