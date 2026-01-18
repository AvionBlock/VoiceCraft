using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.ViewModels.Settings;

public partial class AudioSettingsViewModel : ViewModelBase, IDisposable
{
    private readonly AudioService _audioService;
    private readonly NavigationService _navigationService;
    private readonly NotificationService _notificationService;
    private readonly PermissionsService _permissionsService;
    private readonly SineWaveGenerator _sineWaveGenerator;

    [ObservableProperty] private Data.AudioSettingsViewModel _audioSettings;
    private IDenoiser? _denoiser;
    [ObservableProperty] private bool _detectingVoiceActivity;
    private IAutomaticGainController? _gainController;
    [ObservableProperty] private bool _isPlaying;

    //Testers
    [ObservableProperty] private bool _isRecording;
    [ObservableProperty] private float _microphoneValue;
    private IAudioPlayer? _player;
    private IAudioRecorder? _recorder;

    public AudioSettingsViewModel(
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
        _sineWaveGenerator = new SineWaveGenerator(Constants.SampleRate);

        _audioSettings = new Data.AudioSettingsViewModel(settingsService, _audioService);
    }

    public void Dispose()
    {
        CleanupRecorder();
        CleanupPlayer();
        AudioSettings.Dispose();
        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    private async Task TestRecorder()
    {
        try
        {
            if (await _permissionsService.CheckAndRequestPermission<Permissions.Microphone>(
                    "VoiceCraft requires the microphone permission to be granted in order to test recording!") !=
                PermissionStatus.Granted)
                throw new InvalidOperationException("Could not create recorder, Microphone permission not granted.");

            if (CleanupRecorder()) return;

            _recorder = _audioService.CreateAudioRecorder(Constants.SampleRate, Constants.Channels, Constants.Format);
            _recorder.BufferMilliseconds = Constants.FrameSizeMs;
            _recorder.SelectedDevice = AudioSettings.InputDevice == "Default" ? null : AudioSettings.InputDevice;
            _recorder.OnDataAvailable += OnDataAvailable;
            _recorder.OnRecordingStopped += OnRecordingStopped;
            _recorder.Initialize();

            _gainController = _audioService.GetAutomaticGainController(AudioSettings.AutomaticGainController)
                ?.Instantiate();
            _gainController?.Initialize(_recorder);

            _denoiser = _audioService.GetDenoiser(AudioSettings.Denoiser)?.Instantiate();
            _denoiser?.Initialize(_recorder);

            _recorder.Start();
            IsRecording = true;
        }
        catch (Exception ex)
        {
            IsRecording = false;
            CleanupRecorder();
            _notificationService.SendErrorNotification(ex.Message);
        }
    }

    [RelayCommand]
    private void TestPlayer()
    {
        try
        {
            if (CleanupPlayer()) return;

            _player = _audioService.CreateAudioPlayer(Constants.SampleRate, Constants.Channels, Constants.Format);
            _player.SelectedDevice = AudioSettings.OutputDevice == "Default" ? null : AudioSettings.OutputDevice;
            _player.BufferMilliseconds = 100;
            _player.OnPlaybackStopped += OnPlaybackStopped;
            _player.Initialize((data, count) => ReadSineWave(data.AsSpan(0, count)));
            _player.Play();
            IsPlaying = true;
        }
        catch (Exception ex)
        {
            IsPlaying = false;
            CleanupPlayer();
            _notificationService.SendErrorNotification(ex.Message);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _navigationService.Back();
    }

    private int ReadSineWave(Span<byte> buffer)
    {
        var shortBufferSpan = MemoryMarshal.Cast<byte, short>(buffer);
        var floatBuffer = ArrayPool<float>.Shared.Rent(shortBufferSpan.Length);
        var floatBufferSpan = floatBuffer.AsSpan(0, shortBufferSpan.Length);
        floatBufferSpan.Clear();

        try
        {
            var count = Sample16ToFloat.Read(shortBufferSpan, floatBufferSpan);
            count = _sineWaveGenerator.Read(floatBufferSpan[..count]);
            count = SampleVolume.Read(floatBufferSpan[..count], AudioSettings.OutputVolume);
            count = SampleFloatTo16.Read(floatBufferSpan[..count], shortBufferSpan);
            return count * sizeof(short);
        }
        finally
        {
            ArrayPool<float>.Shared.Return(floatBuffer);
        }
    }

    private void OnDataAvailable(byte[] data, int bytesRead)
    {
        _gainController?.Process(data);
        _denoiser?.Denoise(data);

        var shortBufferSpan = MemoryMarshal.Cast<byte, short>(data)[..(bytesRead / sizeof(short))];
        var floatBuffer = ArrayPool<float>.Shared.Rent(shortBufferSpan.Length);
        var floatBufferSpan = floatBuffer.AsSpan(0, shortBufferSpan.Length);
        floatBufferSpan.Clear();
        var count = Sample16ToFloat.Read(shortBufferSpan, floatBufferSpan);
        var loudness = SampleLoudness.Read(floatBufferSpan[..count]);
        MicrophoneValue = loudness;
        DetectingVoiceActivity = loudness >= AudioSettings.MicrophoneSensitivity;
    }

    private void OnRecordingStopped(Exception? ex)
    {
        CleanupRecorder();

        if (ex != null)
            _notificationService.SendErrorNotification(ex.Message);
    }

    private void OnPlaybackStopped(Exception? ex)
    {
        CleanupPlayer();

        if (ex != null)
            _notificationService.SendErrorNotification(ex.Message);
    }

    private bool CleanupRecorder()
    {
        if (_recorder == null) return false;
        var recorder = _recorder;
        _recorder = null;

        recorder.OnRecordingStopped -= OnRecordingStopped;
        recorder.OnDataAvailable -= OnDataAvailable;
        recorder.Dispose();
        _gainController?.Dispose();
        _denoiser?.Dispose();
        _recorder = null;
        _gainController = null;
        _denoiser = null;
        IsRecording = false;
        MicrophoneValue = 0;
        DetectingVoiceActivity = false;
        return true;
    }

    private bool CleanupPlayer()
    {
        if (_player == null) return false;
        var player = _player;
        _player = null;

        player.OnPlaybackStopped -= OnPlaybackStopped;
        player.Dispose();
        IsPlaying = false;
        return true;
    }

    public override void OnAppearing(object? data = null)
    {
        base.OnAppearing(data);
        _ = AudioSettings.ReloadAvailableDevices();
    }

    public override void OnDisappearing()
    {
        base.OnDisappearing();
        CleanupRecorder();
        CleanupPlayer();
    }
}