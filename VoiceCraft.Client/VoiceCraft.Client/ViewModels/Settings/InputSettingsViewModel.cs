using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using VoiceCraft.Client.Audio;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;
using VoiceCraft.Core.Audio;

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
    private AudioRecorder? _audioRecorder;
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
        _inputSettingsData = new InputSettingsDataViewModel(settingsService, _audioService);

        InputSettingsData.ReloadAvailableDevices();
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
        InputSettingsData.ReloadAvailableDevices();
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
                    "VoiceCraft requires the microphone permission to be granted in order to test recording!") !=
                PermissionStatus.Granted)
                throw new InvalidOperationException("Could not create recorder, Microphone permission not granted.");

            lock (_lock)
            {
                var denoiser = _audioService.GetAudioPreprocessor(InputSettingsData.Denoiser);
                var gainController = _audioService.GetAudioPreprocessor(InputSettingsData.AutomaticGainController);
                _audioPreprocessor = new CombinedAudioPreprocessor(gainController, denoiser, null);
                _audioRecorder = new AudioRecorder();
                _audioRecorder.StartRecording(OnDataAvailable);
                
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


    private void OnDataAvailable(Span<float> buffer)
    {
        _audioPreprocessor?.Process(buffer);
        
        var floatCount = SampleVolume.Read(buffer, InputSettingsData.InputVolume);
        var loudness = SampleLoudness.Read(buffer[..floatCount]);
        MicrophoneValue = loudness;
        DetectingVoiceActivity = loudness >= InputSettingsData.MicrophoneSensitivity;
    }

    //Need to implement.
    private void OnRecordingStopped(Exception? ex)
    {
        CloseRecorder();

        if (ex != null)
            _notificationService.SendErrorNotification(ex.Message);
    }

    private bool CloseRecorder()
    {
        lock (_lock)
        {
            var result = false;
            IsRecording = false;
            MicrophoneValue = 0;
            DetectingVoiceActivity = false;
            
            if (_audioRecorder != null)
            {
                _audioRecorder.StopRecording();
                //Gonna have to dispose.
                result = true;
                _audioRecorder = null;
            }

            _audioPreprocessor?.Dispose();
            _audioPreprocessor = null;
            return result;
        }
    }
}