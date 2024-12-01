using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using VoiceCraft.Client.ViewModels.Home;

namespace VoiceCraft.Client.ViewModels
{
    public partial class HomeViewModel : ViewModelBase
    {
        public override string Title { get; protected set; } = "Home";

        [ObservableProperty]
        private ViewModelBase _content = default!;

        [ObservableProperty]
        private ObservableCollection<ListItemTemplate> _items = new ObservableCollection<ListItemTemplate>();

        [ObservableProperty]
        private ListItemTemplate? _selectedListItem = null;

        public HomeViewModel(ServersViewModel servers, SettingsViewModel settings, CreditsViewModel credits, AddServerViewModel addServer)
        {
            _items.Add(new ListItemTemplate(servers, "home_regular"));
            _items.Add(new ListItemTemplate(settings, "mic_settings_regular"));
            _items.Add(new ListItemTemplate(credits, "book_information_regular"));
            _items.Add(new ListItemTemplate(addServer, "add_regular"));


            SelectedListItem = _items[0];
            _content = _items[0].Content;
        }

        partial void OnSelectedListItemChanged(ListItemTemplate? value)
        {
            if (value == null) return;
            if (Content != null)
                Content.OnDisappearing();

            Content = value.Content;

            if (Content != null)
                Content.OnAppearing();
        }

        [RelayCommand]
        public void Test()
        {
            DialogHost.Show("test", "MessageBoxDialog");
        }
    }

    public class ListItemTemplate
    {
        public ListItemTemplate(ViewModelBase control, string iconKey)
        {
            Content = control;
            Application.Current!.TryFindResource(iconKey, out var icon);
            Icon = (StreamGeometry)icon!;
        }

        public ViewModelBase Content { get; }
        public StreamGeometry? Icon { get; }
    }
}