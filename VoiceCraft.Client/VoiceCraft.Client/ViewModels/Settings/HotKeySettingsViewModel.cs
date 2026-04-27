using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Models;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;
using VoiceCraft.Client.ViewModels.Modals;

namespace VoiceCraft.Client.ViewModels.Settings;

public partial class HotKeySettingsViewModel : ViewModelBase, IDisposable
{
    private readonly NavigationService _navigationService;
    private readonly HotKeySettingsDataViewModel _hotKeySettingsData;

    [ObservableProperty]
    public partial System.Collections.ObjectModel.ObservableCollection<HotKeyActionDataViewModel> HotKeys { get; set; }

    public HotKeySettingsViewModel(NavigationService navigationService, HotKeyService hotKeyService)
    {
        _navigationService = navigationService;
        _hotKeySettingsData = new HotKeySettingsDataViewModel(hotKeyService);
        HotKeys = _hotKeySettingsData.HotKeys;
    }

    public void Dispose()
    {
        _hotKeySettingsData.Dispose();
        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    private void Cancel()
    {
        if (DisableBackButton) return;
        _navigationService.Back();
    }

    [RelayCommand]
    private void StartRebind(HotKeyActionDataViewModel hotKey)
    {
        _navigationService.PushModal<HotKeyCaptureViewModel>(new HotKeyCaptureNavigationData(hotKey));
    }
}