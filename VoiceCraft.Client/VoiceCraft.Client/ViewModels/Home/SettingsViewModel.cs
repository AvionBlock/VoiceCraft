using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Settings;

namespace VoiceCraft.Client.ViewModels.Home;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<ListItemTemplate> _items = [];
    [ObservableProperty] private ListItemTemplate? _selectedListItem;

    public SettingsViewModel(NavigationService navigationService)
    {
        _items.Add(new ListItemTemplate("Settings.General.General",
            () => { navigationService.NavigateTo<GeneralSettingsViewModel>(); }));
        _items.Add(new ListItemTemplate("Settings.Appearance.Appearance",
            () => { navigationService.NavigateTo<AppearanceSettingsViewModel>(); }));
        _items.Add(new ListItemTemplate("Settings.Input.Input",
            () => { navigationService.NavigateTo<InputSettingsViewModel>(); }));
        _items.Add(new ListItemTemplate("Settings.Output.Output",
            () => { navigationService.NavigateTo<OutputSettingsViewModel>(); }));
        _items.Add(new ListItemTemplate("Settings.Network.Network",
            () => { navigationService.NavigateTo<NetworkSettingsViewModel>(); }));
        _items.Add(new ListItemTemplate("Settings.HotKey.HotKey",
            () => { navigationService.NavigateTo<HotKeySettingsViewModel>(); }));
        _items.Add(new ListItemTemplate("Settings.Advanced.Advanced",
            () => { navigationService.NavigateTo<AdvancedSettingsViewModel>(); }));
    }

    partial void OnSelectedListItemChanged(ListItemTemplate? value)
    {
        if (value == null) return;
        value.OnClicked.Invoke();
        _selectedListItem = null;
    }
}

public class ListItemTemplate(string title, Action onClicked)
{
    public string Title { get; } = title;
    public Action OnClicked { get; } = onClicked;
}