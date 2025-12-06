using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave;
using System.Collections.ObjectModel;
using VoiceCraft.Maui.Interfaces;
using VoiceCraft.Maui.Models;

namespace VoiceCraft.Maui.ViewModels;

/// <summary>
/// ViewModel for the settings page.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly IDatabaseService _databaseService;
    private readonly IAudioManager _audioManager;

    [ObservableProperty]
    private bool _isBusy = true;

    [ObservableProperty]
    private SettingsModel _settings = new();

    [ObservableProperty]
    private ObservableCollection<string> _inputDevices = ["Default"];

    [ObservableProperty]
    private ObservableCollection<string> _outputDevices = ["Default"];

    [ObservableProperty]
    private float _microphoneDetection;

    [ObservableProperty]
    private bool _isRecording;

    private IWaveIn? _microphone;
    private readonly WaveFormat _audioFormat = new(48000, 1);

    [ObservableProperty]
    private ObservableCollection<string> _profiles = ["Default", "Low Latency", "High Quality"];

    private string _selectedProfile = "Default";
    public string SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            SetProperty(ref _selectedProfile, value);
            ApplyProfile(value);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    public SettingsViewModel(IDatabaseService databaseService, IAudioManager audioManager)
    {
        _databaseService = databaseService;
        _audioManager = audioManager;
        LoadSettings();

        foreach (var device in _audioManager.GetInputDevices())
            InputDevices.Add(device);

        foreach (var device in _audioManager.GetOutputDevices())
            OutputDevices.Add(device);
    }


        private async void LoadSettings()
        {
            IsBusy = true;
            try
            {
                await _databaseService.Initialization;
                Settings = _databaseService.Settings;

                if (Settings.InputDevice >= _audioManager.GetInputDeviceCount())
                {
                    Settings.InputDevice = 0;
                }
                if (Settings.OutputDevice >= _audioManager.GetOutputDeviceCount())
                {
                    Settings.OutputDevice = 0;
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyProfile(string profile)
        {
            switch (profile)
            {
                case "Default":
                    Settings.Bitrate = 16000;
                    Settings.JitterBufferSize = 80;
                    Settings.UseDtx = true;
                    Settings.NoiseSuppression = false;
                    Settings.SoftLimiterEnabled = true;
                    Settings.DirectionalAudioEnabled = false; 
                    break;
                case "Low Latency":
                    Settings.Bitrate = 16000;
                    Settings.JitterBufferSize = 40;
                    Settings.UseDtx = true;
                    Settings.NoiseSuppression = false;
                    Settings.SoftLimiterEnabled = false;
                    Settings.DirectionalAudioEnabled = false;
                    break;
                case "High Quality":
                    Settings.Bitrate = 48000;
                    Settings.JitterBufferSize = 100;
                    Settings.UseDtx = false;
                    Settings.NoiseSuppression = true;
                    Settings.SoftLimiterEnabled = true;
                    Settings.DirectionalAudioEnabled = true;
                    break;
            }
        }

        [RelayCommand]
        public void ResetToDefaults()
        {
            SelectedProfile = "Default";
            // Also reset keybinds/ports which aren't part of the profile switching logic usually, but are part of "Reset"
            Settings.ClientPort = 8080;
            Settings.ClientSidedPositioning = false;
            Settings.CustomClientProtocol = false;
            Settings.HideAddress = false;
            Settings.LinearVolume = true;
            Settings.MuteKeybind = "LControlKey+M";
            Settings.DeafenKeybind = "LControlKey+LShiftKey+D";
            Settings.SoftLimiterGain = 5.0f;
            Settings.MicrophoneDetectionPercentage = 0.04f;
        }

        [RelayCommand]
        public void SaveSettings()
        {
            if(Microphone != null)
            {
                Microphone.StopRecording();
                Microphone.DataAvailable -= Microphone_DataAvailable;
                Microphone.Dispose();
                Microphone = null;
                MicrophoneDetection = 0;
                IsRecording = false;
            }
            _ = _databaseService.SaveSettings();
        }

        [RelayCommand]
        public async Task OpenCloseMicrophone()
        {
            if (Microphone == null)
            {
                if (!await _audioManager.RequestInputPermissions()) return;
                Microphone = _audioManager.CreateRecorder(AudioFormat, 20);
                Microphone.DataAvailable += Microphone_DataAvailable;
                Microphone.StartRecording();
                IsRecording = true;
            }
            else
            {
                Microphone.StopRecording();
                Microphone.DataAvailable -= Microphone_DataAvailable;
                Microphone.Dispose();
                Microphone = null;
                MicrophoneDetection = 0;
                IsRecording = false;
            }
        }

        private void Microphone_DataAvailable(object? sender, WaveInEventArgs e)
        {
            float max = 0;
            // interpret as 16 bit audio
            for (int index = 0; index < e.BytesRecorded; index += 2)
            {
                short sample = (short)((e.Buffer[index + 1] << 8) |
                                        e.Buffer[index + 0]);
                // to floating point
                var sample32 = sample / 32768f;
                // absolute value 
                if (sample32 < 0) sample32 = -sample32;
                if (sample32 > max) max = sample32;
            }
            MicrophoneDetection = max;
        }
    }
}
