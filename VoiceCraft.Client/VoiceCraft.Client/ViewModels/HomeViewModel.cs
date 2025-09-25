using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.ViewModels.Home;

namespace VoiceCraft.Client.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    [ObservableProperty] private ViewModelBase _content;

    [ObservableProperty] private ObservableCollection<ListItemTemplate> _items = [];

    [ObservableProperty] private ListItemTemplate? _selectedListItem;
    [ObservableProperty] private string _title;

    public HomeViewModel(ServersViewModel servers, SettingsViewModel settings, CreditsViewModel credits,
        CrashLogViewModel crashLog)
    {
        _items.Add(new ListItemTemplate("Home.Servers", servers, "HomeRegular"));
        _items.Add(new ListItemTemplate("Home.Settings", settings, "SettingsRegular"));
        _items.Add(new ListItemTemplate("Home.Credits", credits, "InformationRegular"));
        _items.Add(new ListItemTemplate("Home.CrashLogs", crashLog, "NotebookErrorRegular"));

        SelectedListItem = _items[0];
        _content = _items[0].Content;
        _title = _items[0].Title;
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