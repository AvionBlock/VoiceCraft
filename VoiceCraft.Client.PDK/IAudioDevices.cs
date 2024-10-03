namespace VoiceCraft.Client.PDK
{
    public interface IAudioDevices
    {
        public string DefaultWaveInDevice();
        public string DefaultWaveOutDevice();

        List<string> GetWaveInDevices();
        List<string> GetWaveOutDevices();
    }
}