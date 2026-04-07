using System;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Components;
using VoiceCraft.Client.Audio;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.ViewModels.Settings;

public partial class OutputSettingsViewModel : ViewModelBase, IDisposable
{
    private readonly AudioService _audioService;
    private readonly NavigationService _navigationService;
    private readonly NotificationService _notificationService;
    private readonly Lock _lock = new();

    [ObservableProperty] private OutputSettingsDataViewModel _outputSettingsData;
    [ObservableProperty] private bool _isPlaying;
    private AudioPlaybackDevice? _playbackDevice;
    private IAudioClipper? _audioClipper;

    public OutputSettingsViewModel(
        NavigationService navigationService,
        AudioService audioService,
        NotificationService notificationService,
        SettingsService settingsService)
    {
        _navigationService = navigationService;
        _audioService = audioService;
        _notificationService = notificationService;
        _outputSettingsData = new OutputSettingsDataViewModel(settingsService, _audioService);
    }

    public void Dispose()
    {
        OutputSettingsData.Dispose();
        ClosePlayer();
        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    private void Test()
    {
        lock (_lock)
        {
            if (ClosePlayer())
                return;
            OpenPlayer();
        }
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
        OutputSettingsData.ReloadDevices();
    }

    public override void OnDisappearing()
    {
        base.OnDisappearing();
        ClosePlayer();
    }

    private int Read(Span<float> buffer)
    {
        var read = buffer.Length;
        if (_audioClipper != null)
            read = _audioClipper.Read(buffer);
        read = SampleVolume.Read(buffer[..read], OutputSettingsData.OutputVolume);
        return read;
    }

    private void OpenPlayer()
    {
        try
        {
            lock (_lock)
            {
                _audioClipper = _audioService.GetAudioClipper(OutputSettingsData.AudioClipper)?.Instantiate();
                _playbackDevice = _audioService.InitializePlaybackDevice(
                    Constants.SampleRate,
                    Constants.PlaybackChannels,
                    Constants.FrameSize,
                    OutputSettingsData.OutputDevice);

                var callbackComponent = new CallbackProvider(_playbackDevice.Engine, _playbackDevice.Format, Read);
                callbackComponent.ConnectInput(new Oscillator(_playbackDevice.Engine, _playbackDevice.Format));
                _playbackDevice.MasterMixer.AddComponent(callbackComponent);
                
                _playbackDevice.Start();
                IsPlaying = true;
            }
        }
        catch (Exception ex)
        {
            ClosePlayer();
            _notificationService.SendErrorNotification(
                "Settings.Output.Notification.Badge",
                ex.Message);
        }
    }

    private bool ClosePlayer()
    {
        lock (_lock)
        {
            var result = false;
            IsPlaying = false;

            if (_playbackDevice != null)
            {
                _playbackDevice.Stop();
                _playbackDevice.Dispose();
                _playbackDevice = null;
                result = true;
            }

            _audioClipper = null;
            return result;
        }
    }
}