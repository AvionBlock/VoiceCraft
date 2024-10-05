using NAudio.Wave;
using System.Collections.Generic;
using VoiceCraft.Client.PDK.Audio;

namespace VoiceCraft.Client.Windows.Audio
{
    internal class AudioDevices : IAudioDevices
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
            var devices = new List<string>() { "Default" };
            for (int n = 0; n < WaveIn.DeviceCount; n++)
            {
                var caps = WaveIn.GetCapabilities(n);
                devices.Add(caps.ProductName);
            }

            return devices;
        }

        public List<string> GetWaveOutDevices()
        {
            var devices = new List<string>() { "Default" };
            for (int n = 0; n < WaveOut.DeviceCount; n++)
            {
                var caps = WaveOut.GetCapabilities(n);
                devices.Add(caps.ProductName);
            }

            return devices;
        }
    }
}
