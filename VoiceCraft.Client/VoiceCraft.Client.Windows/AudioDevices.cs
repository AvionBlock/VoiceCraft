using NAudio.Wave;
using System;
using System.Collections.Generic;
using VoiceCraft.Client.PDK;

namespace VoiceCraft.Client.Desktop
{
    internal class AudioDevices : IAudioDevices
    {
        public List<string> GetWaveInDevices()
        {
            var devices = new List<string>();
            for (int n = -1; n < WaveIn.DeviceCount; n++)
            {
                var caps = WaveIn.GetCapabilities(n);
                devices.Add(caps.ProductName);
            }

            return devices;
        }

        public List<string> GetWaveOutDevices()
        {
            var devices = new List<string>();
            for (int n = -1; n < WaveOut.DeviceCount; n++)
            {
                var caps = WaveOut.GetCapabilities(n);
                devices.Add(caps.ProductName);
            }

            return devices;
        }
    }
}
