using NAudio.Wave;

namespace VoiceCraft.Client.PDK.Audio
{
    public partial interface IAudioPlayer : IWavePlayer
    {
        int DesiredLatency { get; set; }

        void SetDevice(string device);

        string GetDefaultDevice();

        List<string> GetDevices();
    }
}