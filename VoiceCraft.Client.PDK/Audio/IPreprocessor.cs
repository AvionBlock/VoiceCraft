namespace VoiceCraft.Client.PDK.Audio
{
    public interface IPreprocessor
    {
        bool IsGainControllerAvailable { get; }

        bool IsNoiseSuppressorAvailable { get; }
    }
}
