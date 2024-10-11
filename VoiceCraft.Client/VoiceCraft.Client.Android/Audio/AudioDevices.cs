using Android.Media;
using Android.OS;
using System.Collections.Generic;
using System.Linq;
using VoiceCraft.Client.PDK.Audio;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Android.Audio
{
    public class AudioDevices : IAudioDevices
    {
        private AudioManager? _audioManager;

        public AudioDevices(AudioManager? audioManager = null)
        {
            _audioManager = audioManager;
        }

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
            //There is no way we can switch input devices yet, but we can actually retrieve them.
            return new List<string>()
            {
                "Default"
            };
        }

        public List<string> GetWaveOutDevices()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S && _audioManager != null)
            {
#pragma warning disable CA1416
                //Since api Version 31, We can select individual output devices like on desktop.
                var devices = _audioManager.AvailableCommunicationDevices.Select(x => $"{x.ProductName.Truncate(8)} - {x.Type}").ToList();
                devices.Insert(0, "Default");
                if (devices != null)
                    return devices;
#pragma warning restore CA1416
            }
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
