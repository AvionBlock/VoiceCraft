using Android.Media;
using Android.OS;

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

        private void Reset()
        {
            _audioManager.Mode = Mode.Normal;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            {
#pragma warning disable CA1416
                _audioManager.ClearCommunicationDevice();
#pragma warning restore CA1416
            }
            else if(Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
#pragma warning disable CA1422
                _audioManager.StopBluetoothSco();
                _audioManager.BluetoothScoOn = false;
                _audioManager.SpeakerphoneOn = false;
#pragma warning restore CA1422
            }
        }

        public void PlayWithPhone()
        {
            Reset();
            _audioManager.Mode = Mode.InCommunication;
        }

        public void PlayWithSpeaker()
        {
            Reset();
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            {
#pragma warning disable CA1416
#pragma warning restore CA1416
            }
            else if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
#pragma warning disable CA1422
                _audioManager.SpeakerphoneOn = true;
#pragma warning restore CA1422
            }
        }

        public void PlayWithBluetooth()
        {
            _audioManager.Mode = Mode.InCommunication;
        }
    }
}
