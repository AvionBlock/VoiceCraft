using NAudio.Wave;
using System.Collections.Generic;
using VoiceCraft.Client.PDK.Audio;
using VoiceCraft.Client.PDK.Services;

namespace VoiceCraft.Client.Windows.Audio
{
    public class NativeAudioService : AudioService
    {
        public override IAudioRecorder CreateAudioRecorder()
        {
            return new AudioRecorder();
        }

        public override IAudioPlayer CreateAudioPlayer()
        {
            return new AudioPlayer();
        }

        public override string GetDefaultInputDevice()
        {
            return "Default";
        }

        public override string GetDefaultOutputDevice()
        {
            return "Default";
        }

        public override string GetDefaultPreprocessor()
        {
            return "None";
        }

        public override string GetDefaultEchoCanceller()
        {
            return "None";
        }

        public override List<string> GetInputDevices()
        {
            var devices = new List<string>() { GetDefaultInputDevice() };
            for (int n = 0; n < WaveIn.DeviceCount; n++)
            {
                var caps = WaveIn.GetCapabilities(n);
                if (!devices.Contains(caps.ProductName))
                    devices.Add(caps.ProductName);
            }

            return devices;
        }

        public override List<string> GetOutputDevices()
        {
            var devices = new List<string>() { GetDefaultOutputDevice() };
            for (int n = 0; n < WaveOut.DeviceCount; n++)
            {
                var caps = WaveOut.GetCapabilities(n);
                if (!devices.Contains(caps.ProductName))
                    devices.Add(caps.ProductName);
            }

            return devices;
        }

        public override List<string> GetPreprocessors()
        {
            var preprocessors = new List<string>() { GetDefaultPreprocessor() };
            preprocessors.AddRange(_registeredPreprocessors.Keys);
            return preprocessors;
        }

        public override List<string> GetEchoCancellers()
        {
            var echoCancellers = new List<string>() { GetDefaultEchoCanceller() };
            echoCancellers.AddRange(_registeredEchoCancellers.Keys);
            return echoCancellers;
        }
    }
}
