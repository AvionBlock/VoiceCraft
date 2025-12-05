using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Maui.Models
{
    public partial class SettingsModel : ObservableObject
    {
        [ObservableProperty]
        int inputDevice = 0;
        [ObservableProperty]
        int outputDevice = 0;
        [ObservableProperty]
        int clientPort = 8080;
        [ObservableProperty]
        int jitterBufferSize = 80;
        [ObservableProperty]
        float softLimiterGain = 5.0f;
        [ObservableProperty]
        float microphoneDetectionPercentage = 0.04f;
        [ObservableProperty]
        bool directionalAudioEnabled;
        [ObservableProperty]
        bool clientSidedPositioning;
        [ObservableProperty]
        bool customClientProtocol;
        [ObservableProperty]
        bool linearVolume = true;
        [ObservableProperty]
        bool softLimiterEnabled = true;
        [ObservableProperty]
        bool hideAddress;
        [ObservableProperty]
        string muteKeybind = "LControlKey+M";
        [ObservableProperty]
        string deafenKeybind = "LControlKey+LShiftKey+D";
        
        [ObservableProperty]
        int bitrate = 16000; // 16kbps default for good voice quality/low bandwidth
        
        [ObservableProperty]
        bool useDtx = true; // Variable bitrate/discontinuous transmission
        
        [ObservableProperty]
        bool noiseSuppression = false;
    }
}
