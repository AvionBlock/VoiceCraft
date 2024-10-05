using System.Collections.Generic;
using VoiceCraft.Client.PDK.Audio;

namespace VoiceCraft.Client.Android.Audio
{
    public class AudioDevices : IAudioDevices
    {
        public string DefaultWaveInDevice()
        {
            return "Default";
        }

        public string DefaultWaveOutDevice()
        {
            return "Default";
        }

        public List<string> GetWaveInDevices()
        {
            return new List<string>()
            {
                "Default",
                "Microphone",
                "Camcorder",
                "Voice Communication"
            };
        }

        public List<string> GetWaveOutDevices()
        {
            return new List<string>()
            {
                "Default",
                "Phone",
                "Speaker",
                "Bluetooth"
            };
        }
    }
}
