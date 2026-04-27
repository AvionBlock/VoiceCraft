using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Models;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;

namespace VoiceCraft.Client.ViewModels.Modals;

public partial class HotKeyCaptureViewModel(NavigationService navigationService, HotKeyService hotKeyService)
    : ViewModelBase
{
    public override bool DisableBackButton => true;
    private readonly HotKeySettingsDataViewModel _hotKeySettingsData = new(hotKeyService);
    private HotKeyActionDataViewModel? _hotKeyAction;
    [ObservableProperty] public partial string RebindingTitle { get; set; } = string.Empty;
    [ObservableProperty] public partial string RebindingPreview { get; set; } = string.Empty;

    public override void OnAppearing(object? data = null)
    {
        if (data is not HotKeyCaptureNavigationData navigationData) return;
        _hotKeyAction = navigationData.HotKey;
        RebindingTitle = _hotKeyAction.Title;
        RebindingPreview = _hotKeyAction.Keybind;
    }

    [RelayCommand]
    private void ConfirmRebind()
    {
        if (string.IsNullOrWhiteSpace(RebindingPreview) ||
            string.IsNullOrWhiteSpace(_hotKeyAction?.Action.Id)) return;
        var action = _hotKeyAction;
        if (action == null) return;

        _hotKeySettingsData.SetBinding(action.Action,
            HotKeyService.NormalizeKeyCombo(RebindingPreview.Replace(" + ", "\0")));
        _hotKeyAction.Keybind = RebindingPreview;
        CancelRebind();
    }

    [RelayCommand]
    private void UpdateBindingPreview(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var keys = text.Split(" + ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        RebindingPreview = HotKeyService.NormalizeKeyCombo(keys).Replace("\0", " + ");
    }

    [RelayCommand]
    private void CancelRebind()
    {
        navigationService.PopModal();
    }

    [RelayCommand]
    private void ClearBindingPreview()
    {
        RebindingPreview = string.Empty;
    }
}