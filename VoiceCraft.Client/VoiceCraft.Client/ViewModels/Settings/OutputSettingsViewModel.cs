using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private readonly SineWaveGenerator _sineWaveGenerator;
    private readonly Lock _lock = new();

    [ObservableProperty] private OutputSettingsDataViewModel _outputSettingsData;
    [ObservableProperty] private bool _isPlaying;
    private IAudioPlayer? _audioPlayer;
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
        _sineWaveGenerator = new SineWaveGenerator(Constants.SampleRate);

        _ = OutputSettingsData.ReloadAvailableDevices();
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
        _ = OutputSettingsData.ReloadAvailableDevices();
    }

    public override void OnDisappearing()
    {
        base.OnDisappearing();
        ClosePlayer();
    }

    private int Read(byte[] buffer, int length)
    {
        var shortSpanBuffer = MemoryMarshal.Cast<byte, short>(buffer)[..(length / sizeof(short))];
        var floatBuffer = ArrayPool<float>.Shared.Rent(shortSpanBuffer.Length);
        var floatSpanBuffer = floatBuffer.AsSpan(0, shortSpanBuffer.Length);
        floatSpanBuffer.Clear();
        try
        {
            var floatCount = Sample16ToFloat.Read(shortSpanBuffer, floatSpanBuffer);
            floatCount = _sineWaveGenerator.Read(floatSpanBuffer[..floatCount]);
            if(_audioClipper != null)
                floatCount = _audioClipper.Read(floatSpanBuffer[..floatCount]);
            floatCount = SampleVolume.Read(floatSpanBuffer[..floatCount], OutputSettingsData.OutputVolume);
            return SampleFloatTo16.Read(floatSpanBuffer[..floatCount], shortSpanBuffer) * sizeof(short);
        }
        finally
        {
            ArrayPool<float>.Shared.Return(floatBuffer);
        }
    }

    private void OpenPlayer()
    {
        try
        {
            lock (_lock)
            {
                _audioClipper = _audioService.GetAudioClipper(OutputSettingsData.AudioClipper)?.Instantiate();
                _audioPlayer =
                    _audioService.CreateAudioPlayer(Constants.SampleRate, Constants.Channels, Constants.Format);
                _audioPlayer.SelectedDevice =
                    OutputSettingsData.OutputDevice == "Default" ? null : OutputSettingsData.OutputDevice;
                _audioPlayer.BufferMilliseconds = 100;
                _audioPlayer.OnPlaybackStopped += OnPlaybackStopped;
                _audioPlayer.Initialize(Read);
                _audioPlayer.Play();
                IsPlaying = true;
            }
        }
        catch(Exception ex)
        {
            ClosePlayer();
            _notificationService.SendErrorNotification(ex.Message);
        }
    }
    
    
    private void OnPlaybackStopped(Exception? ex)
    {
        ClosePlayer();

        if (ex != null)
            _notificationService.SendErrorNotification(ex.Message);
    }

    private bool ClosePlayer()
    {
        lock (_lock)
        {
            if (_audioPlayer == null) return false;
            _audioPlayer.OnPlaybackStopped -= OnPlaybackStopped;
            IsPlaying = false;
            if (_audioPlayer.PlaybackState == PlaybackState.Playing)
                _audioPlayer.Stop();
            _audioClipper = null;
            _audioPlayer.Dispose();
            _audioPlayer = null;
            return true;
        }
    }
}