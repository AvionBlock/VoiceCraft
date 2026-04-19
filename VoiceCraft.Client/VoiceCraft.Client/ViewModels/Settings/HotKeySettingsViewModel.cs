using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.ViewModels.Settings;

public partial class HotKeySettingsViewModel : ViewModelBase, IDisposable
{
    private readonly NavigationService _navigationService;
    private readonly HotKeySettingsDataViewModel _hotKeySettingsData;
    private string? _rebindActionId;

    [ObservableProperty]
    public partial System.Collections.ObjectModel.ObservableCollection<HotKeyActionDataViewModel> HotKeys { get; set; }

    [ObservableProperty] public partial bool IsRebinding { get; set; }
    [ObservableProperty] public partial string RebindingTitle { get; set; } = string.Empty;
    [ObservableProperty] public partial string RebindingPreview { get; set; } = string.Empty;

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
        if (IsRebinding) return;
        _rebindActionId = hotKey.Action.Id;
        IsRebinding = true;
        DisableBackButton = true;
        RebindingTitle = Localizer.Get(hotKey.Title);
        RebindingPreview = hotKey.Keybind;
    }

    [RelayCommand]
    private void UpdateBindingPreview(string? text)
    {
        if (!IsRebinding || string.IsNullOrWhiteSpace(text)) return;
        var keys = text.Split(" + ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        RebindingPreview = HotKeyService.NormalizeKeyCombo(keys).Replace("\0", " + ");
    }

    [RelayCommand]
    private void ClearBindingPreview()
    {
        if (!IsRebinding) return;
        RebindingPreview = string.Empty;
    }

    [RelayCommand]
    private void ConfirmRebind()
    {
        if (!IsRebinding || string.IsNullOrWhiteSpace(RebindingPreview) ||
            string.IsNullOrWhiteSpace(_rebindActionId)) return;
        var action = HotKeys.FirstOrDefault(x => x.Action.Id == _rebindActionId)?.Action;
        if (action == null) return;
        _hotKeySettingsData.SetBinding(action, HotKeyService.NormalizeKeyCombo(RebindingPreview.Replace(" + ", "\0")));
        HotKeys = _hotKeySettingsData.HotKeys;
        CancelRebind();
    }

    [RelayCommand]
    private void CancelRebind()
    {
        _rebindActionId = null;
        IsRebinding = false;
        DisableBackButton = false;
        RebindingTitle = string.Empty;
        RebindingPreview = string.Empty;
    }
}