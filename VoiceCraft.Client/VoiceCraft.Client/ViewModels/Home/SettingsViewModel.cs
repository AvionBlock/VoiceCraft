using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Settings;

namespace VoiceCraft.Client.ViewModels.Home;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty] public partial ObservableCollection<ListItemTemplate> Items { get; set; }
    [ObservableProperty] public partial ListItemTemplate? SelectedListItem { get; set; }

    public SettingsViewModel(NavigationService navigationService)
    {
        Items =
        [
            new ListItemTemplate("Settings.General.Title",
                () => { navigationService.NavigateTo<GeneralSettingsViewModel>(); }),
            new ListItemTemplate("Settings.Appearance.Title",
                () => { navigationService.NavigateTo<AppearanceSettingsViewModel>(); }),
            new ListItemTemplate("Settings.Input.Title",
                () => { navigationService.NavigateTo<InputSettingsViewModel>(); }),
            new ListItemTemplate("Settings.Output.Title",
                () => { navigationService.NavigateTo<OutputSettingsViewModel>(); }),
            new ListItemTemplate("Settings.Network.Title",
                () => { navigationService.NavigateTo<NetworkSettingsViewModel>(); }),
            new ListItemTemplate("Settings.HotKey.Title",
                () => { navigationService.NavigateTo<HotKeySettingsViewModel>(); }),
            new ListItemTemplate("Settings.Advanced.Title",
                () => { navigationService.NavigateTo<AdvancedSettingsViewModel>(); })
        ];
    }

    partial void OnSelectedListItemChanged(ListItemTemplate? value)
    {
        if (value == null) return;
        value.OnClicked.Invoke();
        SelectedListItem = null;
    }
}

public class ListItemTemplate(string title, Action onClicked)
{
    public string Title { get; } = title;
    public Action OnClicked { get; } = onClicked;
}