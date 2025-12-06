using NAudio.Wave;
using VoiceCraft.Maui.Interfaces;

namespace VoiceCraft.Maui
{
    public class AudioManager : IAudioManager
    {
        public static AudioManager Instance { get; } = new AudioManager(new Models.SettingsModel());
        
        public AudioManager(Models.SettingsModel settings)
        {
        }

        public IWavePlayer CreatePlayer(ISampleProvider AudioFormat)
        {
            throw new NotImplementedException();
        }

        public IWaveIn CreateRecorder(WaveFormat AudioFormat, int bufferMS)
        {
            throw new NotImplementedException();
        }

        public string[] GetInputDevices()
        {
            throw new NotImplementedException();
        }

        public string[] GetOutputDevices()
        {
            throw new NotImplementedException();
        }

        public int GetInputDeviceCount()
        {
            throw new NotImplementedException();
        }

        public int GetOutputDeviceCount()
        {
            throw new NotImplementedException();
        }

        public async Task<bool> RequestInputPermissions()
        {
            var status = await Permissions.RequestAsync<Permissions.Microphone>();
            if (Permissions.ShouldShowRationale<Permissions.Microphone>())
            {
                Shell.Current.DisplayAlert("Error", "VoiceCraft requires the microphone to communicate with other users!", "OK").Wait();
                return false;
            }

            return status == PermissionStatus.Granted;
        }
    }
}
