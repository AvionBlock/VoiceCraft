using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Enums;
using VoiceCraft.Client.Audio;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.ViewModels.Settings;

public partial class InputSettingsViewModel : ViewModelBase, IDisposable
{
    private readonly AudioService _audioService;
    private readonly NavigationService _navigationService;
    private readonly NotificationService _notificationService;
    private readonly PermissionsService _permissionsService;
    private readonly Lock _lock = new();

    [ObservableProperty] private InputSettingsDataViewModel _inputSettingsData;
    [ObservableProperty] private bool _isRecording;
    [ObservableProperty] private float _microphoneValue;
    [ObservableProperty] private bool _detectingVoiceActivity;
    private AudioCaptureDevice? _captureDevice;
    private CombinedAudioPreprocessor? _audioPreprocessor;

    public InputSettingsViewModel(
        NavigationService navigationService,
        AudioService audioService,
        NotificationService notificationService,
        PermissionsService permissionsService,
        SettingsService settingsService)
    {
        _navigationService = navigationService;
        _audioService = audioService;
        _notificationService = notificationService;
        _permissionsService = permissionsService;
        _inputSettingsData = new InputSettingsDataViewModel(settingsService, audioService);
    }

    public void Dispose()
    {
        InputSettingsData.Dispose();
        CloseRecorder();
        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    private void Test()
    {
        if (CloseRecorder())
            return;
        _ = OpenRecorder();
    }

    [RelayCommand]
    private void Cancel()
    {
        if (DisableBackButton) return;
        _navigationService.Back();
    }

    public override void OnAppearing(object? data = null)
    {
        base.OnAppearing(data);
        InputSettingsData.ReloadDevices();
    }

    public override void OnDisappearing()
    {
        base.OnDisappearing();
        CloseRecorder();
    }

    private async Task OpenRecorder()
    {
        try
        {
            if (await _permissionsService.CheckAndRequestPermission<Permissions.Microphone>(
                    Localizer.Get("Permissions.Microphone.RequiredForTesting")) !=
                PermissionStatus.Granted)
                throw new InvalidOperationException(Localizer.Get("Permissions.Microphone.NotGranted"));

            lock (_lock)
            {
                var denoiser = _audioService.GetAudioPreprocessor(InputSettingsData.Denoiser);
                var gainController = _audioService.GetAudioPreprocessor(InputSettingsData.AutomaticGainController);
                _audioPreprocessor = new CombinedAudioPreprocessor(gainController, denoiser, null);
                _captureDevice = _audioService.InitializeCaptureDevice(
                    Constants.SampleRate,
                    Constants.RecordingChannels,
                    Constants.FrameSize,
                    InputSettingsData.InputDevice);
                _captureDevice.Start();
                _captureDevice.OnAudioProcessed += Write;
                IsRecording = true;
            }
        }
        catch (Exception ex)
        {
            CloseRecorder();
            // ReSharper disable once InconsistentlySynchronizedField
            _notificationService.SendErrorNotification(ex.Message);
        }
    }


    private void Write(Span<float> buffer, Capability _)
    {
        _audioPreprocessor?.Process(buffer);

        var floatCount = SampleVolume.Read(buffer, InputSettingsData.InputVolume);
        var loudness = SampleLoudness.Read(buffer[..floatCount]);
        MicrophoneValue = loudness;
        DetectingVoiceActivity = loudness >= InputSettingsData.MicrophoneSensitivity;
    }

    private bool CloseRecorder()
    {
        lock (_lock)
        {
            var result = false;
            IsRecording = false;
            MicrophoneValue = 0;
            DetectingVoiceActivity = false;

            if (_captureDevice != null)
            {
                _captureDevice.Stop();
                _captureDevice.Dispose();
                _captureDevice = null;
                result = true;
            }

            _audioPreprocessor?.Dispose();
            _audioPreprocessor = null;
            return result;
        }
    }
}
