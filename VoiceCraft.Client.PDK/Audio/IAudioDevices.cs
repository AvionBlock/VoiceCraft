namespace VoiceCraft.Client.PDK.Audio
{
    public interface IAudioDevices
    {
        public string DefaultWaveInDevice();
        public string DefaultWaveOutDevice();

        List<string> GetWaveInDevices();
        List<string> GetWaveOutDevices();
    }
}