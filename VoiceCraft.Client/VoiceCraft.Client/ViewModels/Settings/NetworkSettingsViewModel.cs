using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;
using VoiceCraft.Network;

namespace VoiceCraft.Client.ViewModels.Settings;

public partial class NetworkSettingsViewModel(
    NavigationService navigationService,
    SettingsService settingsService)
    : ViewModelBase, IDisposable
{
    //Network Settings
    [ObservableProperty]
    public partial NetworkSettingsDataViewModel NetworkSettingsData { get; set; } = new(settingsService);

    [ObservableProperty]
    public partial PositioningType[] PositioningTypes { get; set; } = Enum.GetValues<PositioningType>();

    public void Dispose()
    {
        NetworkSettingsData.Dispose();
        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    private void Cancel()
    {
        if (DisableBackButton) return;
        navigationService.Back();
    }
}