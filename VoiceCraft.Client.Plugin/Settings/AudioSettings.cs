using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.PDK.Services;

namespace VoiceCraft.Client.Plugin.Settings
{
    public partial class AudioSettings : Setting<AudioSettings>
    {
        [ObservableProperty]
        private string? _inputDevice;
        [ObservableProperty]
        private string? _outputDevice;
        [ObservableProperty]
        private float _microphoneSensitivity = 0.04f;
        [ObservableProperty]
        private float _microphoneGain = 1.0f;
    }
}
