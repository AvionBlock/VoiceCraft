using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;

namespace VoiceCraft.Client.ViewModels.Settings;

public partial class HotKeySettingsViewModel : ViewModelBase, IDisposable
{
    private readonly NavigationService _navigationService;
    private readonly HotKeySettingsDataViewModel _hotKeySettingsData;

    [ObservableProperty] private System.Collections.ObjectModel.ObservableCollection<HotKeyActionDataViewModel> _hotKeys;
    [ObservableProperty] private bool _isRebinding;
    [ObservableProperty] private string _rebindingTitle = string.Empty;

    public HotKeySettingsViewModel(NavigationService navigationService, HotKeyService hotKeyService)
    {
        _navigationService = navigationService;
        _hotKeySettingsData = new HotKeySettingsDataViewModel(hotKeyService);
        _hotKeys = _hotKeySettingsData.HotKeys;
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
        if (IsRebinding) return;
        IsRebinding = true;
        DisableBackButton = true;
        RebindingTitle = hotKey.Title;
    }

    [RelayCommand]
    private void CaptureBinding(string? text)
    {
        if (!IsRebinding || string.IsNullOrWhiteSpace(text)) return;
        var action = HotKeys.FirstOrDefault(x => x.Title == RebindingTitle)?.Action;
        if (action == null) return;
        var keys = text.Split(" + ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        _hotKeySettingsData.SetBinding(action, HotKeyService.NormalizeKeyCombo(keys));
        HotKeys = _hotKeySettingsData.HotKeys;
        IsRebinding = false;
        DisableBackButton = false;
        RebindingTitle = string.Empty;
    }
}
