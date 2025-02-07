using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Jeek.Avalonia.Localization;
using VoiceCraft.Client.ViewModels.Home;

namespace VoiceCraft.Client.ViewModels
{
    public partial class HomeViewModel : ViewModelBase
    {
        [ObservableProperty] private string _title;

        [ObservableProperty] private ViewModelBase _content;

        [ObservableProperty] private ObservableCollection<ListItemTemplate> _items = [];

        [ObservableProperty] private ListItemTemplate? _selectedListItem;

        public HomeViewModel(ServersViewModel servers, SettingsViewModel settings, CreditsViewModel credits, CrashLogViewModel crashLog)
        {
            Localizer.LanguageChanged += LanguageChanged;
            _items.Add(new ListItemTemplate("Home.Servers", servers, "HomeRegular"));
            _items.Add(new ListItemTemplate("Home.Settings", settings, "SettingsRegular"));
            _items.Add(new ListItemTemplate("Home.Credits", credits, "InformationRegular"));
            _items.Add(new ListItemTemplate("Home.CrashLogs", crashLog, "NotebookErrorRegular"));

            SelectedListItem = _items[0];
            _content = _items[0].Content;
            _title = Localizer.Get(_items[0].Title);
        }

        partial void OnSelectedListItemChanged(ListItemTemplate? value)
        {
            if (value == null) return;
            if (Content is { } viewModel)
                viewModel.OnDisappearing();

            Content = value.Content;
            Title = Localizer.Get(value.Title);
            Content.OnAppearing();
        }

        private void LanguageChanged(object? sender, EventArgs e)
        {
            Title = Localizer.Get(SelectedListItem?.Title ?? "N.A.");
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
}