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
        [ObservableProperty] private string _title;
        
        [ObservableProperty]
        private ViewModelBase _content;

        [ObservableProperty]
        private ObservableCollection<ListItemTemplate> _items = [];

        [ObservableProperty]
        private ListItemTemplate? _selectedListItem;

        public HomeViewModel(ServersViewModel servers, SettingsViewModel settings, CreditsViewModel credits, AddServerViewModel addServer)
        {
            _items.Add(new ListItemTemplate("Servers", servers, "HomeRegular"));
            _items.Add(new ListItemTemplate("Settings", settings, "MicSettingsRegular"));
            _items.Add(new ListItemTemplate("Credits", credits, "BookInformationRegular"));
            _items.Add(new ListItemTemplate("Add Server", addServer, "AddRegular"));


            SelectedListItem = _items[0];
            _content = _items[0].Content;
            _title = _items[0].Title;
        }

        partial void OnSelectedListItemChanged(ListItemTemplate? value)
        {
            if (value == null) return;
            if(Content is { } viewModel)
                viewModel.OnDisappearing();
            
            Content = value.Content;
            Title = value.Title;
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