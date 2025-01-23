using NAudio.Wave;

namespace VoiceCraft.Client.Audio.Interfaces
{
    public interface IAudioPlayer : IWavePlayer
    {
        int DesiredLatency { get; set; }
        
        public string? SelectedDevice { get; set; }
    }
}