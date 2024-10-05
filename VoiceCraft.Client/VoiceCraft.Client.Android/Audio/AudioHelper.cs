using Android.Media;
using System.Diagnostics;

namespace VoiceCraft.Client.Android.Audio
{
    public class AudioHelper
    {
        private AudioManager _audioManager;

        public AudioHelper(MainActivity activity)
        {
            var manager = (AudioManager?)activity.GetSystemService(MainActivity.AudioService);
            if (manager == null) throw new System.Exception("Audio service could not be found!");
            _audioManager = manager;
        }

        public void PlayWithPhone()
        {
            _audioManager.Mode = Mode.InCall;
        }

        public void PlayWithSpeaker()
        {
            var devices = _audioManager.GetDevices(GetDevicesTargets.Outputs);
            foreach(var device in devices)
            {
                Debug.WriteLine(device.ProductName);
            }
        }

        public void PlayWithBluetooth()
        {
            _audioManager.Mode = Mode.InCommunication;
        }
    }
}
