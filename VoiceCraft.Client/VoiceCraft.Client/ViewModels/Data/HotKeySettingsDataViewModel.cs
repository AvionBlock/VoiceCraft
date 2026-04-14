using System;
using System.Collections.ObjectModel;
using System.Linq;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels.Data;

public class HotKeySettingsDataViewModel : IDisposable
{
    private readonly HotKeyService _hotKeyService;
    private readonly HotKeySettings _hotKeySettings;

    public HotKeySettingsDataViewModel(HotKeyService hotKeyService, SettingsService settingsService)
    {
        _hotKeyService = hotKeyService;
        _hotKeySettings = settingsService.HotKeySettings;
        _hotKeyService.OnBindingsChanged += Reload;
        HotKeys = new ObservableCollection<HotKeyActionDataViewModel>(
            _hotKeyService.GetBindings().Select(x => new HotKeyActionDataViewModel(x.Action, x.KeyCombo)));
    }

    public ObservableCollection<HotKeyActionDataViewModel> HotKeys { get; private set; }

    public bool PlayPushToTalkCues
    {
        get => _hotKeySettings.PlayPushToTalkCues;
        set => _hotKeySettings.PlayPushToTalkCues = value;
    }

    public void Dispose()
    {
        _hotKeyService.OnBindingsChanged -= Reload;
        GC.SuppressFinalize(this);
    }

    public void SetBinding(HotKeyAction action, string keyCombo)
    {
        _hotKeyService.SetBinding(action.Id, keyCombo);
    }

    private void Reload()
    {
        HotKeys = new ObservableCollection<HotKeyActionDataViewModel>(
            _hotKeyService.GetBindings().Select(x => new HotKeyActionDataViewModel(x.Action, x.KeyCombo)));
    }
}
