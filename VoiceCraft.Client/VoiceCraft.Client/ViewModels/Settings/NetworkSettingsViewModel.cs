using System;
using System.Collections.ObjectModel;
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
    public partial ObservableCollection<PositioningTypeValue> PositioningTypes { get; set; } =
    [
        new("Settings.Network.PositioningType.Server", PositioningType.Server),
        new("Settings.Network.PositioningType.Client", PositioningType.Client)
    ];

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

    public record PositioningTypeValue(string Title, PositioningType Value);
}