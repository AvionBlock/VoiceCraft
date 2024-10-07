using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using VoiceCraft.Client.PDK.ViewModels;
using VoiceCraft.Client.PDK.Views;
using VoiceCraft.Client.Plugin.Views.Home;

namespace VoiceCraft.Client.Plugin.ViewModels
{
    public partial class HomeViewModel : ViewModelBase
    {
        public override string Title => "Home";

        [ObservableProperty]
        private ViewBase _content = default!;

        [ObservableProperty]
        private ObservableCollection<ListItemTemplate> _items = new ObservableCollection<ListItemTemplate>();

        [ObservableProperty]
        private ListItemTemplate? _selectedListItem = null;

        public HomeViewModel(ServersView servers, SettingsView settings, CreditsView credits, AddServerView addServer, PluginsView plugins)
        {
            _items.Add(new ListItemTemplate(servers, "home_regular"));
            _items.Add(new ListItemTemplate(settings, "mic_settings_regular"));
            _items.Add(new ListItemTemplate(credits, "book_information_regular"));
            _items.Add(new ListItemTemplate(addServer, "add_regular"));
            _items.Add(new ListItemTemplate(plugins, "extension_regular"));


            SelectedListItem = _items[0];
            _content = _items[0].Content;
        }

        partial void OnSelectedListItemChanged(ListItemTemplate? value)
        {
            if (value == null) return;
            if (Content != null && Content.DataContext is ViewModelBase currentViewModel)
                currentViewModel.OnDisappearing(this);

            Content = value.Content;

            if (Content != null && Content.DataContext is ViewModelBase newViewModel)
                newViewModel.OnAppearing(this);
        }
    }

    public class ListItemTemplate
    {
        public ListItemTemplate(ViewBase control, string iconKey)
        {
            Content = control;
            Application.Current!.TryFindResource(iconKey, out var icon);
            Icon = (StreamGeometry)icon!;
        }

        public ViewBase Content { get; }
        public StreamGeometry? Icon { get; }
    }
}