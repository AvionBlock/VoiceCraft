using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.Interfaces;

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
    private IAudioRecorder? _audioRecorder;
    private IDenoiser? _denoiser;
    private IAutomaticGainController? _gainController;

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

        _ = InputSettingsData.ReloadAvailableDevices();
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
        _ = InputSettingsData.ReloadAvailableDevices();
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
                _denoiser = _audioService.GetDenoiser(InputSettingsData.Denoiser)?.Instantiate();
                _gainController = _audioService.GetAutomaticGainController(InputSettingsData.AutomaticGainController)
                    ?.Instantiate();
                _audioRecorder =
                    _audioService.CreateAudioRecorder(Constants.SampleRate, Constants.Channels, Constants.Format);
                _audioRecorder.BufferMilliseconds = Constants.FrameSizeMs;
                _audioRecorder.SelectedDevice =
                    InputSettingsData.InputDevice == "Default" ? null : InputSettingsData.InputDevice;
                _audioRecorder.Initialize();
                _denoiser?.Initialize(_audioRecorder);
                _gainController?.Initialize(_audioRecorder);
                _audioRecorder.Start();
                
                _audioRecorder.OnDataAvailable += OnDataAvailable;
                _audioRecorder.OnRecordingStopped += OnRecordingStopped;
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


    private void OnDataAvailable(byte[] buffer, int length)
    {
        _gainController?.Process(buffer);
        _denoiser?.Denoise(buffer);
        
        var shortSpanBuffer = MemoryMarshal.Cast<byte, short>(buffer)[..(length / sizeof(short))];
        var floatBuffer = ArrayPool<float>.Shared.Rent(shortSpanBuffer.Length);
        var floatSpanBuffer = floatBuffer.AsSpan(0, shortSpanBuffer.Length);
        floatSpanBuffer.Clear();

        try
        {
            var floatCount = Sample16ToFloat.Read(shortSpanBuffer, floatSpanBuffer);
            floatCount = SampleVolume.Read(floatSpanBuffer[..floatCount], InputSettingsData.InputVolume);
            var loudness = SampleLoudness.Read(floatSpanBuffer[..floatCount]);
            MicrophoneValue = loudness;
            DetectingVoiceActivity = loudness >= InputSettingsData.MicrophoneSensitivity;
        }
        finally
        {
            ArrayPool<float>.Shared.Return(floatBuffer);
        }
    }

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
                _audioRecorder.OnDataAvailable -= OnDataAvailable;
                _audioRecorder.OnRecordingStopped -= OnRecordingStopped;
                if (_audioRecorder.CaptureState == CaptureState.Capturing)
                    _audioRecorder.Stop();
                _audioRecorder.Dispose();
                result = true;
            }

            _denoiser?.Dispose();
            _gainController?.Dispose();
            _audioRecorder = null;
            _denoiser = null;
            _gainController = null;
            return result;
        }
    }
}