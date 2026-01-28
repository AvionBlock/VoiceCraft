using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;

namespace VoiceCraft.Client.ViewModels.Settings;

public partial class OutputSettingsViewModel : ViewModelBase, IDisposable
{
    private readonly AudioService _audioService;
    private readonly NavigationService _navigationService;
    private readonly NotificationService _notificationService;
    private readonly PermissionsService _permissionsService;
    
    [ObservableProperty] private OutputSettingsDataViewModel _outputSettingsData;
    
    public OutputSettingsViewModel(
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
        _outputSettingsData = new OutputSettingsDataViewModel(settingsService, _audioService);
        
        _ = OutputSettingsData.ReloadAvailableDevices();
    }
    
    public void Dispose()
    {
        OutputSettingsData.Dispose();
        GC.SuppressFinalize(this);
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
}