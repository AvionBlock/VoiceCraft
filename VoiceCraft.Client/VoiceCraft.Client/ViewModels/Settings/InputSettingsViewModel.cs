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

namespace VoiceCraft.Client.ViewModels.Settings;

public partial class InputSettingsViewModel(
    NavigationService navigationService,
    AudioService audioService,
    NotificationService notificationService,
    PermissionsService permissionsService,
    SettingsService settingsService)
    : ViewModelBase, IDisposable
{
    private readonly Lock _lock = new();

    [ObservableProperty] private InputSettingsDataViewModel _inputSettingsData = new(settingsService, audioService);
    [ObservableProperty] private bool _isRecording;
    [ObservableProperty] private float _microphoneValue;
    [ObservableProperty] private bool _detectingVoiceActivity;
    private AudioCaptureDevice? _captureDevice;
    private CombinedAudioPreprocessor? _audioPreprocessor;

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
        navigationService.Back();
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
            if (await permissionsService.CheckAndRequestPermission<Permissions.Microphone>() != PermissionStatus.Granted)
                throw new PermissionException("Settings.Input.Permissions.MicrophoneNotGranted");

            lock (_lock)
            {
                var denoiser = audioService.GetAudioPreprocessor(InputSettingsData.Denoiser);
                var gainController = audioService.GetAudioPreprocessor(InputSettingsData.AutomaticGainController);
                _audioPreprocessor = new CombinedAudioPreprocessor(gainController, denoiser, null);
                _captureDevice = audioService.InitializeCaptureDevice(
                    Constants.SampleRate,
                    Constants.RecordingChannels,
                    Constants.FrameSize,
                    InputSettingsData.InputDevice,
                    InputSettingsData.InputCapturePreset);
                _captureDevice.Start();
                _captureDevice.OnAudioProcessed += Write;
                IsRecording = true;
            }
        }
        catch (Exception ex)
        {
            CloseRecorder();
            // ReSharper disable once InconsistentlySynchronizedField
            notificationService.SendErrorNotification(
                "Settings.Input.Notification.Badge", 
                ex.Message);
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
