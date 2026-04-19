using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.ViewModels.Home;

namespace VoiceCraft.Client.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    [ObservableProperty] public partial ViewModelBase Content { get; set; }
    [ObservableProperty] public partial ObservableCollection<ListItemTemplate> Items { get; set; }
    [ObservableProperty] public partial ListItemTemplate? SelectedListItem { get; set; }
    [ObservableProperty] public partial string Title { get; set; }

    public HomeViewModel(ServersViewModel servers, SettingsViewModel settings, CreditsViewModel credits,
        CrashLogViewModel crashLog)
    {
        Items =
        [
            new ListItemTemplate("Servers.Title", servers, "HomeRegular"),
            new ListItemTemplate("Settings.Title", settings, "SettingsRegular"),
            new ListItemTemplate("Credits.Title", credits, "InformationRegular"),
            new ListItemTemplate("CrashLogs.Title", crashLog, "NotebookErrorRegular")
        ];

        SelectedListItem = Items[0];
        Content = Items[0].Content;
        Title = Items[0].Title;
    }

    partial void OnSelectedListItemChanged(ListItemTemplate? value)
    {
        if (value == null) return;
        if (Content is { } viewModel)
            viewModel.OnDisappearing();

        Content = value.Content;
        Title = value.Title;
        Content.OnAppearing();
    }
}

public class ListItemTemplate
{
    public ListItemTemplate(string title, ViewModelBase control, string iconKey)
    {
        Content = control;
        Application.Current!.TryFindResource(iconKey, out var icon);
        Title = title;
        Icon = (StreamGeometry?)icon;
    }

    public string Title { get; }
    public ViewModelBase Content { get; }
    public StreamGeometry? Icon { get; }
}