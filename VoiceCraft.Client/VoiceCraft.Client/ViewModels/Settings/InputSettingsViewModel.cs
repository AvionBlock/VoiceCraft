using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;

namespace VoiceCraft.Client.ViewModels.Settings;

public partial class InputSettingsViewModel : ViewModelBase, IDisposable
{
    private readonly AudioService _audioService;
    private readonly NavigationService _navigationService;
    private readonly NotificationService _notificationService;
    private readonly PermissionsService _permissionsService;
    
    [ObservableProperty] private InputSettingsDataViewModel _inputSettingsData;
    
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
        _ = InputSettingsData.ReloadAvailableDevices();
    }
}