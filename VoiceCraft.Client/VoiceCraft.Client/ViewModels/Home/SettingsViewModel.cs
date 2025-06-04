using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Settings;

namespace VoiceCraft.Client.ViewModels.Home;

public partial class SettingsViewModel(NavigationService navigationService) : ViewModelBase
{
    [RelayCommand]
    private void GoToGeneralSettings()
    {
        navigationService.NavigateTo<GeneralSettingsViewModel>();
    }

    [RelayCommand]
    private void GoToAppearanceSettings()
    {
        navigationService.NavigateTo<AppearanceSettingsViewModel>();
    }

    [RelayCommand]
    private void GoToAudioSettings()
    {
        navigationService.NavigateTo<AudioSettingsViewModel>();
    }

    [RelayCommand]
    private void GoToNetworkSettings()
    {
        navigationService.NavigateTo<NetworkSettingsViewModel>();
    }

    [RelayCommand]
    private void GoToAdvancedSettings()
    {
        navigationService.NavigateTo<AdvancedSettingsViewModel>();
    }
}