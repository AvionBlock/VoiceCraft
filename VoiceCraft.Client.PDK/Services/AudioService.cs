using VoiceCraft.Client.PDK.Audio;

namespace VoiceCraft.Client.PDK.Services
{
    public abstract class AudioService
    {
        public readonly IAudioRecorder SharedRecorder;
        public readonly IAudioPlayer SharedPlayer;

        public AudioService()
        {
            SharedRecorder = CreateAudioRecorder();
            SharedPlayer = CreateAudioPlayer();
        }

        public abstract string GetDefaultInputDevice();

        public abstract string GetDefaultOutputDevice();

        public abstract List<string> GetInputDevices();

        public abstract List<string> GetOutputDevices();

        public abstract IAudioRecorder CreateAudioRecorder();

        public abstract IAudioPlayer CreateAudioPlayer();

        public abstract object CreateEchoCanceller();

        public abstract object CreatePreprocessor();
    }
}
