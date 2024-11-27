using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.PDK.Services;

namespace VoiceCraft.Client.Plugin.Settings
{
    public partial class AudioSettings : Setting
    {
        [ObservableProperty]
        private string _inputDevice = "Default";
        [ObservableProperty]
        private string _outputDevice = "Default";
        [ObservableProperty]
        private string _preprocessor = "None";
        [ObservableProperty]
        private string _echoCanceller = "None";
        [ObservableProperty]
        private float _microphoneSensitivity = 0.04f;
        [ObservableProperty]
        private float _microphoneGain = 1.0f;
        [ObservableProperty]
        private bool _aec = false;
        [ObservableProperty]
        private bool _agc = false;
        [ObservableProperty]
        private bool _denoiser = false;
        [ObservableProperty]
        private bool _vad = false;

        public override object Clone()
        {
            return MemberwiseClone();
        }
    }
}