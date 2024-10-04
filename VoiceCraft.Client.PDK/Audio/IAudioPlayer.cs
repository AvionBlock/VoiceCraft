using NAudio.Wave;

namespace VoiceCraft.Client.PDK.Audio
{
    public partial interface IAudioPlayer : IWavePlayer
    {
        int Latency { get; set; }

        void SetDevice(string device);
    }
}