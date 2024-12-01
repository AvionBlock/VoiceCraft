using CommunityToolkit.Mvvm.ComponentModel;
using System;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Settings
{
    public partial class AudioSettings : Setting
    {
        [ObservableProperty]
        private string _inputDevice = "Default";
        [ObservableProperty]
        private string _outputDevice = "Default";
        [ObservableProperty]
        private Guid _preprocessor = Guid.Empty;
        [ObservableProperty]
        private Guid _echoCanceler = Guid.Empty;
        [ObservableProperty]
        private float _microphoneSensitivity = 0.04f;
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
