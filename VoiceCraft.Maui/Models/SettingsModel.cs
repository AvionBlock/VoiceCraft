using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Maui.Models;

/// <summary>
/// Observable model representing application settings.
/// </summary>
public partial class SettingsModel : ObservableObject
{
    [ObservableProperty]
    private int _inputDevice;

    [ObservableProperty]
    private int _outputDevice;

    [ObservableProperty]
    private int _clientPort = 8080;

    [ObservableProperty]
    private int _jitterBufferSize = 80;

    [ObservableProperty]
    private float _softLimiterGain = 5.0f;

    [ObservableProperty]
    private float _microphoneDetectionPercentage = 0.04f;

    [ObservableProperty]
    private bool _directionalAudioEnabled;

    [ObservableProperty]
    private bool _clientSidedPositioning;

    [ObservableProperty]
    private bool _customClientProtocol;

    [ObservableProperty]
    private bool _linearVolume = true;

    [ObservableProperty]
    private bool _softLimiterEnabled = true;

    [ObservableProperty]
    private bool _hideAddress;

    [ObservableProperty]
    private string _muteKeybind = "LControlKey+M";

    [ObservableProperty]
    private string _deafenKeybind = "LControlKey+LShiftKey+D";

    /// <summary>16kbps default for good voice quality/low bandwidth.</summary>
    [ObservableProperty]
    private int _bitrate = 16000;

    /// <summary>Variable bitrate/discontinuous transmission.</summary>
    [ObservableProperty]
    private bool _useDtx = true;

    [ObservableProperty]
    private bool _noiseSuppression;
}

