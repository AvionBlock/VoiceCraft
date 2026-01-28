using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;

namespace VoiceCraft.Client.ViewModels.Settings;

public partial class HotKeySettingsViewModel(NavigationService navigationService, HotKeyService hotKeyService)
    : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<HotKeyActionDataViewModel> _hotKeys =
        new(hotKeyService.HotKeyActions.Select(x => new HotKeyActionDataViewModel(x.Value, x.Key)));

    [RelayCommand]
    private void Cancel()
    {
        if (DisableBackButton) return;
        navigationService.Back();
    }
}