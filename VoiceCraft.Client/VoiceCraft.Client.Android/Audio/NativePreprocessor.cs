using Android.Media.Audiofx;
using VoiceCraft.Client.PDK.Audio;

namespace VoiceCraft.Client.Android.Audio
{
    public class NativePreprocessor : IPreprocessor
    {
        public bool IsGainControllerAvailable => AutomaticGainControl.IsAvailable;
        public bool IsNoiseSuppressorAvailable => NoiseSuppressor.IsAvailable;
    }
}