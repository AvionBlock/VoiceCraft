using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;

namespace VoiceCraft.Client.ViewModels.Settings;

public partial class NetworkSettingsViewModel(
    NavigationService navigationService,
    SettingsService settingsService)
    : ViewModelBase, IDisposable
{
    //Network Settings
    [ObservableProperty] private Data.NetworkSettingsViewModel _networkSettings = new(settingsService);
    [ObservableProperty] private PositioningType[] _positioningTypes = Enum.GetValues<PositioningType>();

    public void Dispose()
    {
        NetworkSettings.Dispose();
        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    private void Cancel()
    {
        if (DisableBackButton) return;
        navigationService.Back();
    }
}