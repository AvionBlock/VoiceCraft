namespace VoiceCraft.Client.PDK
{
    public interface IAudioDevices
    {
        List<string> GetWaveInDevices();
        List<string> GetWaveOutDevices();
    }
}